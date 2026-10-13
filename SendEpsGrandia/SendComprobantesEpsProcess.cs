using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SendEpsGrandia.Entities;
using SendEpsGrandia.Helpers;
using SendEpsGrandia.Repositories;
using SendEpsGrandia.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SendEpsGrandia
{
    /// <summary>
    /// Procesa el envío de comprobantes (facturas y notas de crédito) a la EPS en segundo plano.
    /// Implementa limitación estricta de concurrencia mediante SemaphoreSlim y 
    /// un patrón Circuit Breaker con ventana de enfriamiento (Cooldown) ante caídas masivas de red.
    /// </summary>
    public class SendComprobantesEpsProcess : GenericMethods
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Convert.ToInt32(ConfigurationManager.AppSettings["timeout_EPS"] ?? "180"))
        };

        private static readonly object _fileLock = new object();

        // Control de concurrencia de hilos
        private static SemaphoreSlim _semaphoreComprobantes;
        private static readonly object _semaphoreLock = new object();
        private static int _fallosConsecutivosRed = 0;
        private static DateTime _ultimoFalloRed = DateTime.MinValue;

        private const string LOG_TOKEN_ERR = "ErrorComprobantesEPS - 901_Token";
        private const string LOG_TIMEOUT_ERR = "ErrorComprobantesEPS - 902_TimeoutRed";
        private const string LOG_FORMATO_ERR = "ErrorComprobantesEPS - 903_FormatoHtml";
        private const string LOG_GLOBAL_ERR = "ErrorComprobantesEPS - 999_FatalGlobal";

        /// <summary>
        /// Método principal (Orquestador).
        /// 1. Verifica estado del Circuit Breaker y aplica ventana de enfriamiento si corresponde.
        /// 2. Inicializa el límite de hilos en caliente.
        /// 3. Solicita a la BD exactamente los registros correspondientes a los hilos libres.
        /// 4. Despacha tareas en paralelo protegiendo el flujo ante caídas de red.
        /// </summary>
        public void ExecuteProcess()
        {
            try
            {
                EPSPrintDA epsDA = new EPSPrintDA();

                // 1. EVALUAR CIRCUIT BREAKER CON VENTANA DE ENFRIAMIENTO (60 SEGUNDOS)
                if (Interlocked.CompareExchange(ref _fallosConsecutivosRed, 0, 0) >= 3)
                {
                    if ((DateTime.Now - _ultimoFalloRed).TotalSeconds >= 60)
                    {
                        Interlocked.Exchange(ref _fallosConsecutivosRed, 0);
                        LogControl.save(LOG_TIMEOUT_ERR, "Circuit Breaker: Se ha restablecido el contador tras superar la ventana de enfriamiento (60s). Reanudando envíos.", "1");
                    }
                    else
                    {
                        // Esperar a que concluya el periodo de enfriamiento sin sobrecargar la base de datos
                        return;
                    }
                }

                // 2. INICIALIZAR Y ACTUALIZAR SEMÁFORO DINÁMICAMENTE
                string paramHilos = epsDA.GetParamConfig("HILOS_COMPROBANTE_EPS");
                int maxLimit = (int.TryParse(paramHilos, out int val) && val > 0) ? val : 5;

                if (_semaphoreComprobantes == null)
                {
                    lock (_semaphoreLock)
                    {
                        if (_semaphoreComprobantes == null)
                        {
                            _semaphoreComprobantes = new SemaphoreSlim(maxLimit, maxLimit);
                        }
                    }
                }

                // 3. CONSULTAR ESPACIOS DISPONIBLES AHORA
                int slotsDisponibles = _semaphoreComprobantes.CurrentCount;

                if (slotsDisponibles <= 0)
                    return;

                // 4. SOLICITAR EXACTAMENTE LA CANTIDAD DE SLOTS LIBRES A BD
                List<ComprobantesEpsBM> comprobantesList = epsDA.GetJobsSendComprobantesEPS(slotsDisponibles);

                if (comprobantesList == null || comprobantesList.Count == 0)
                    return;

                string epsUser = epsDA.GetParamConfig("EPSUser");
                string epsPwd = epsDA.GetParamConfig("EPSoPwd");
                string urlTokenEpsSctr = epsDA.GetParamConfig("urlTokenEPS_Fact_SCTR");
                string urlComprobantesEps = epsDA.GetParamConfig("urlComprobanteEPS_SCTR");
                int timeoutEps = Convert.ToInt32(ConfigurationManager.AppSettings["timeout_EPS"] ?? "180");

                // 5. BUCLE PARALELO LIMITADO POR SEMÁFORO
                foreach (ComprobantesEpsBM item in comprobantesList)
                {
                    // Validación preventiva de Circuit Breaker durante la iteración
                    if (Interlocked.CompareExchange(ref _fallosConsecutivosRed, 0, 0) >= 3)
                    {
                        string msgAbort = "Se abortó el despacho del lote tras registrarse 3 fallos consecutivos hacia la EPS.";
                        LogControl.save(LOG_TIMEOUT_ERR, "Circuit Breaker Activado: " + msgAbort, "3");
                        epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, msgAbort, "1", item.NID_PROC);
                        break;
                    }

                    // Intentar tomar un hilo disponible
                    if (!_semaphoreComprobantes.Wait(0))
                        break;

                    // Despachar a un hilo en segundo plano
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string mToken = await ObtenerTokenEpsAsync(urlTokenEpsSctr, epsUser, epsPwd);

                            if (string.IsNullOrEmpty(mToken))
                            {
                                string errToken = "La EPS devolvió una respuesta vacía o nula al solicitar el token.";
                                epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 4);
                                epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlTokenEpsSctr, errToken, "2", item.NID_PROC);
                                LogControl.save(LOG_TOKEN_ERR, $"Token Vacío - NRECEIPT: {item.NRECEIPT} | " + errToken, "3");

                                _ultimoFalloRed = DateTime.Now;
                                Interlocked.Increment(ref _fallosConsecutivosRed);
                                return;
                            }

                            if (!EsJsonValido(mToken))
                            {
                                string errHtmlToken = $"Error de Formato: El token devuelto no es JSON. Contenido recibido: {mToken}";
                                epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 4);
                                epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlTokenEpsSctr, errHtmlToken, mToken, item.NID_PROC);
                                LogControl.save(LOG_TOKEN_ERR, $"Token HTML/Inválido - NRECEIPT: {item.NRECEIPT} | " + errHtmlToken, "3");

                                _ultimoFalloRed = DateTime.Now;
                                Interlocked.Increment(ref _fallosConsecutivosRed);
                                return;
                            }

                            var sQuoteResult = JsonConvert.DeserializeObject<AuthEPSResult>(mToken);

                            if (sQuoteResult != null && string.Equals(sQuoteResult.success, "true", StringComparison.OrdinalIgnoreCase))
                            {
                                bool envioExitoso = await EnviarComprobanteEpsAsync(item, urlComprobantesEps, sQuoteResult.token, epsDA);

                                if (envioExitoso)
                                {
                                    // Reseteo seguro de contador de fallos
                                    Interlocked.Exchange(ref _fallosConsecutivosRed, 0);
                                }
                                else
                                {
                                    // Registro de fallo de transmisión
                                    _ultimoFalloRed = DateTime.Now;
                                    Interlocked.Increment(ref _fallosConsecutivosRed);
                                }
                            }
                            else
                            {
                                string errAuth = $"Autenticación rechazada por la EPS. JSON recibido: {mToken}";
                                epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 4);
                                epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlTokenEpsSctr, errAuth, mToken, item.NID_PROC);
                                LogControl.save(LOG_TOKEN_ERR, $"Auth EPS Falló - NRECEIPT: {item.NRECEIPT} | " + errAuth, "3");
                            }
                        }
                        catch (TaskCanceledException exTimeout)
                        {
                            _ultimoFalloRed = DateTime.Now;
                            Interlocked.Increment(ref _fallosConsecutivosRed);

                            string errTimeout = $"Timeout de red ({timeoutEps}s / 504 Gateway Timeout). Detalle: {exTimeout.Message}";
                            epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 3);
                            epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, errTimeout, "5", item.NID_PROC);
                            LogControl.save(LOG_TIMEOUT_ERR, $"Timeout EPS ({timeoutEps}s) - NRECEIPT: {item.NRECEIPT} | " + exTimeout.ToString(), "3");
                        }
                        catch (Exception ex)
                        {
                            _ultimoFalloRed = DateTime.Now;
                            Interlocked.Increment(ref _fallosConsecutivosRed);

                            epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 3);
                            epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, ex.ToString(), "6", item.NID_PROC);
                            LogControl.save(LOG_GLOBAL_ERR, $"Excepción Item - NRECEIPT: {item.NRECEIPT} | " + ex.ToString(), "3");
                        }
                        finally
                        {
                            // Liberar el slot garantizando que el siguiente Timer pueda tomar el hilo
                            _semaphoreComprobantes.Release();
                        }
                    });
                }
            }
            catch (Exception exGlobal)
            {
                LogControl.save(LOG_GLOBAL_ERR, "Fatal Error Global en ExecuteProcess | " + exGlobal.ToString(), "3");
            }
        }

        private async Task<string> ObtenerTokenEpsAsync(string urlToken, string usuario, string clave)
        {
            var credentials = new { usuario, clave };
            string jsonCredentials = JsonConvert.SerializeObject(credentials);

            using (var requestToken = new HttpRequestMessage(HttpMethod.Post, urlToken))
            {
                requestToken.Content = new StringContent(jsonCredentials, Encoding.UTF8, "application/json");
                using (HttpResponseMessage responseToken = await _httpClient.SendAsync(requestToken).ConfigureAwait(false))
                {
                    if (responseToken.IsSuccessStatusCode)
                    {
                        return await responseToken.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
            }
            return string.Empty;
        }

        private async Task<bool> EnviarComprobanteEpsAsync(ComprobantesEpsBM item, string urlComprobantesEps, string token, EPSPrintDA epsDA)
        {
            var formatObject = new
            {
                codigoProcesoTransaccion = item.NID_PROC,
                TipoComprobanteEmitir = item.SBILLTYPE,
                SerieComprobanteEmitir = item.NINSUR_AREA,
                NumeroComprobanteEmitir = item.NBILLNUM,
                TipoComprobanteNC = string.IsNullOrEmpty(item.SBILLTYPE_O) ? null : item.SBILLTYPE_O,
                SerieComprobanteNC = string.IsNullOrEmpty(item.NINSUR_AREA_O) ? null : item.NINSUR_AREA_O,
                NumeroComprobanteNC = string.IsNullOrEmpty(item.NBILLNUM_O) ? null : item.NBILLNUM_O
            };

            string jsonComprobante = JsonConvert.SerializeObject(formatObject);
            epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "01 - Se envia el JSON a la EPS - Comprobante", urlComprobantesEps, jsonComprobante, "3", item.NID_PROC);

            using (var requestComprobante = new HttpRequestMessage(HttpMethod.Post, urlComprobantesEps))
            {
                requestComprobante.Content = new StringContent(jsonComprobante, Encoding.UTF8, "application/json");
                requestComprobante.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using (HttpResponseMessage responseComprobante = await _httpClient.SendAsync(requestComprobante).ConfigureAwait(false))
                {
                    string sQuoteResult2 = await responseComprobante.Content.ReadAsStringAsync().ConfigureAwait(false);
                    epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, sQuoteResult2, "4", item.NID_PROC);

                    if (!responseComprobante.IsSuccessStatusCode)
                    {
                        string errorDetalle = $"Error HTTP Infraestructura: {(int)responseComprobante.StatusCode} ({responseComprobante.ReasonPhrase}). Respuesta del servidor: {sQuoteResult2}";
                        epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 5);
                        epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, errorDetalle, sQuoteResult2, item.NID_PROC);
                        LogControl.save(LOG_TIMEOUT_ERR, $"Error HTTP {responseComprobante.StatusCode} - NRECEIPT: {item.NRECEIPT} | " + errorDetalle, "3");
                        return false;
                    }

                    if (!EsJsonValido(sQuoteResult2))
                    {
                        string errorHtml = $"Error Formato: Se esperaba JSON pero se recibió HTML o texto plano. Contenido: {sQuoteResult2}";
                        epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 5);
                        epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, errorHtml, sQuoteResult2, item.NID_PROC);
                        LogControl.save(LOG_FORMATO_ERR, $"Formato Inválido (No JSON) - NRECEIPT: {item.NRECEIPT} | " + errorHtml, "3");
                        return false;
                    }

                    var responseObject = JObject.Parse(sQuoteResult2.Trim());
                    bool isSuccess = responseObject["success"]?.Value<bool>() ?? false;
                    string messageResponse = responseObject["message"]?.Value<string>() ?? string.Empty;

                    // Validación de Idempotencia: Verificar si el comprobante ya existía en la EPS previamente
                    bool esVentaExistente = !isSuccess && !string.IsNullOrWhiteSpace(messageResponse) && messageResponse.IndexOf("Ya existe una venta", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isSuccess || esVentaExistente)
                    {
                        epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 1);
                        return true;
                    }
                    else
                    {
                        epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 5);
                        return true;
                    }
                }
            }
        }

        private bool EsJsonValido(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            string trimmed = texto.Trim();
            return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                   (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
        }
    }
}