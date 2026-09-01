using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SendEpsGrandia.Entities;
using SendEpsGrandia.Helpers;
using SendEpsGrandia.Repositories;
using SendEpsGrandia.Util;
using System;
using System.Collections.Generic;
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
    /// un patrón "Circuit Breaker" para abortar lotes ante caídas masivas de red.
    /// </summary>
    public class SendComprobantesEpsProcess : GenericMethods
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(Convert.ToInt32(GetValueConfig("timeout_EPS"))) };

        private static readonly object _fileLock = new object();

        // Control de concurrencia de hilos
        private static SemaphoreSlim _semaphoreComprobantes;
        private static readonly object _semaphoreLock = new object();
        private static int _currentMaxLimit = 0;

        private const string LOG_TOKEN_ERR = "ErrorComprobantesEPS - 901_Token";
        private const string LOG_TIMEOUT_ERR = "ErrorComprobantesEPS - 902_TimeoutRed";
        private const string LOG_FORMATO_ERR = "ErrorComprobantesEPS - 903_FormatoHtml";
        private const string LOG_GLOBAL_ERR = "ErrorComprobantesEPS - 999_FatalGlobal";

        /// <summary>
        /// Método principal (Orquestador).
        /// 1. Inicializa el límite de hilos en caliente.
        /// 2. Solicita a la BD exactamente los registros correspondientes a los hilos libres.
        /// 3. Despacha tareas en paralelo (Fire-and-Forget) protegiendo el flujo con un Circuit Breaker.
        /// </summary>
        public void ExecuteProcess()
        {
            try
            {
                EPSPrintDA epsDA = new EPSPrintDA();

                // 1. INICIALIZAR Y ACTUALIZAR SEMÁFORO DINÁMICAMENTE
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

                // 2. CONSULTAR ESPACIOS DISPONIBLES AHORA
                int slotsDisponibles = _semaphoreComprobantes.CurrentCount;

                if (slotsDisponibles <= 0)
                    return;

                //var resultadoLimpieza = epsDA.ActualizarEstadoComprobantesEPS();

                // 3. SOLICITAR EXACTAMENTE LA CANTIDAD DE SLOTS LIBRES A BD
                List<ComprobantesEpsBM> comprobantesList = epsDA.GetJobsSendComprobantesEPS(slotsDisponibles);

                if (comprobantesList == null || comprobantesList.Count == 0)
                    return;

                string epsUser = epsDA.GetParamConfig("EPSUser");
                string epsPwd = epsDA.GetParamConfig("EPSoPwd");
                string urlTokenEpsSctr = epsDA.GetParamConfig("urlTokenEPS_Fact_SCTR");
                string urlComprobantesEps = epsDA.GetParamConfig("urlComprobanteEPS_SCTR");
                int timeoutEps = Convert.ToInt32(GetValueConfig("timeout_EPS"));

                int fallosConsecutivosRed = 0;

                // 4. BUCLE PARALELO LIMITADO POR SEMÁFORO
                foreach (ComprobantesEpsBM item in comprobantesList)
                {
                    // Circuit Breaker (Lectura segura en multihilo)
                    if (Interlocked.CompareExchange(ref fallosConsecutivosRed, 0, 0) >= 3)
                    {
                        string msgAbort = "Se abortó el lote tras 3 fallos consecutivos de red o timeout hacia la EPS.";
                        LogControl.save(LOG_TIMEOUT_ERR, "Circuit Breaker Activado: " + msgAbort, "3");
                        epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, msgAbort, "1", item.NID_PROC);
                        break;
                    }

                    // Intentar tomar un hilo (si no hay, rompemos el bucle)
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
                                LogControl.save(LOG_TOKEN_ERR, $"Token Vacío - NRECEIPT: {item.NRECEIPT}" + errToken, "3");
                                return;
                            }

                            if (!EsJsonValido(mToken))
                            {
                                string errHtmlToken = $"Error de Formato: El token devuelto no es JSON. Contenido recibido: {mToken}";
                                epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 4);
                                epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlTokenEpsSctr, errHtmlToken, mToken, item.NID_PROC);
                                LogControl.save(LOG_TOKEN_ERR, $"Token HTML/Inválido - NRECEIPT: {item.NRECEIPT} " + errHtmlToken, "3");
                                return;
                            }

                            var sQuoteResult = JsonConvert.DeserializeObject<AuthEPSResult>(mToken);

                            if (sQuoteResult != null && string.Equals(sQuoteResult.success, "true", StringComparison.OrdinalIgnoreCase))
                            {
                                bool envioExitoso = await EnviarComprobanteEpsAsync(item, urlComprobantesEps, sQuoteResult.token, epsDA);

                                if (envioExitoso)
                                {
                                    // Reseteo seguro de contador de fallos
                                    Interlocked.Exchange(ref fallosConsecutivosRed, 0);
                                }
                                else
                                {
                                    // Incremento seguro de fallos
                                    Interlocked.Increment(ref fallosConsecutivosRed);
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
                            Interlocked.Increment(ref fallosConsecutivosRed);
                            string errTimeout = $"Timeout de red ({timeoutEps}s / 504 Gateway Timeout). Detalle: {exTimeout.Message}";
                            epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 3);
                            epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, errTimeout, "5", item.NID_PROC);
                            LogControl.save(LOG_TIMEOUT_ERR, $"Timeout EPS ({timeoutEps}s) - NRECEIPT: {item.NRECEIPT} | " + exTimeout.ToString(), "3");
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref fallosConsecutivosRed);
                            epsDA.UpdStatusComprobantesEPS(item, item.NTIMES, 3);
                            epsDA.InsLogAuthEps(Convert.ToInt64(item.P_NID_COTIZACION), "02 - Se recibe respuesta de la EPS - Comprobante", urlComprobantesEps, ex.ToString(), "6", item.NID_PROC);
                            LogControl.save(LOG_GLOBAL_ERR, $"Excepción Item - NRECEIPT: {item.NRECEIPT} | " + ex.ToString(), "3");
                        }
                        finally
                        {
                            // 5. SE LIBERA EL SLOT SIEMPRE (Incluso ante excepciones)
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

        /// <summary>
        /// Solicita un token Bearer temporal al servicio de autenticación de la EPS.
        /// </summary>
        /// <returns>La respuesta JSON en formato texto o un string vacío en caso de fallo.</returns>
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

        /// <summary>
        /// Realiza el formateo de los datos del comprobante y ejecuta el POST a la API destino de la EPS.
        /// Gestiona los escenarios de error, timeout y valida si el comprobante ya existía en la EPS previamente.
        /// </summary>
        /// <returns>Retorna True si el envío fue exitoso o el documento ya existe en destino, False si falló la transmisión.</returns>
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

        /// <summary>
        /// Valida de manera rápida si una cadena de texto tiene la estructura básica de un JSON válido.
        /// </summary>
        private bool EsJsonValido(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            string trimmed = texto.Trim();
            return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                   (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
        }
    }
}