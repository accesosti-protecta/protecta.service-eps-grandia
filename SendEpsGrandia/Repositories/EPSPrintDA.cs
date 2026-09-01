using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SendEpsGrandia.Entities;
using SendEpsGrandia.Helpers;
using SendEpsGrandia.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace SendEpsGrandia.Repositories
{
    public class EPSPrintDA : GenericMethods
    {
        private const string LOG_PARAM_ERR = "ErrorSendDataEPS - 900_Parametros";
        private const string LOG_TIMEOUT_ERR = "ErrorSendDataEPS - 902_TimeoutRed";
        private const string LOG_FORMATO_ERR = "ErrorSendDataEPS - 903_FormatoHtml";
        private const string LOG_GLOBAL_ERR = "ErrorSendDataEPS - 999_ExcepcionGeneral";
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        static EPSPrintDA()
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        public string GetParamConfig(string param)
        {
            string result = string.Empty;
            string connectionString = ConfigurationManager.ConnectionStrings["Conexion"].ConnectionString;

            using (OracleConnection cn = new OracleConnection(connectionString))
            using (OracleCommand cmd = new OracleCommand("SP_REA_PARAM_CONFIG", cn))
            {
                try
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_STYPE", OracleDbType.Varchar2, param, ParameterDirection.Input);

                    var pSData = new OracleParameter("P_SDATA", OracleDbType.Varchar2, null, ParameterDirection.Output);
                    pSData.Size = 100;
                    cmd.Parameters.Add(pSData);

                    cn.Open();
                    cmd.ExecuteNonQuery();
                    result = pSData.Value != null && pSData.Value != DBNull.Value ? pSData.Value.ToString() : string.Empty;
                }
                catch (Exception ex)
                {
                    LogControl.save("GetParamConfig", $"Error BD Parámetro: {param} | " + ex.ToString(), "3");
                    result = string.Empty;
                }
            }

            return result;
        }


        public List<EPSJobVM> GetJobList(int cantidadSolicitada)
        {
            List<EPSJobVM> jobsList = new List<EPSJobVM>();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.sp_SendEPSJob;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_CANTIDAD_SOLICITADA", OracleDbType.Int32, cantidadSolicitada, ParameterDirection.Input);
                        cmd.Parameters.Add("P_COD_ERR", OracleDbType.Int32, ParameterDirection.Output);
                        cmd.Parameters.Add("P_SMESSAGE", OracleDbType.Varchar2, 4000, null, ParameterDirection.Output);
                        cmd.Parameters.Add("C_TABLE", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        cn.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                EPSJobVM item = new EPSJobVM();
                                item.NIDHEADERPROC = reader["NIDHEADERPROC"] == DBNull.Value ? 0 : Convert.ToInt64(reader["NIDHEADERPROC"]);
                                jobsList.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogControl.save("GetJobList", ex.ToString(), "3");
                        jobsList = new List<EPSJobVM>();
                    }
                }
            }

            return jobsList;
        }


        public List<ComprobantesEpsBM> GetJobsSendComprobantesEPS(int cantidadSolicitada)
        {
            List<ComprobantesEpsBM> response = new List<ComprobantesEpsBM>();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        IDataReader reader = null;
                        cmd.CommandText = GenericProcedures.sp_GetComprobantesJob;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_CANTIDAD_SOLICITADA", OracleDbType.Int32, cantidadSolicitada, ParameterDirection.Input);
                        cmd.Parameters.Add("P_COD_ERR", OracleDbType.Int32, ParameterDirection.Output);
                        cmd.Parameters.Add("P_SMESSAGE", OracleDbType.Varchar2, 4000, null, ParameterDirection.Output);
                        cmd.Parameters.Add("C_TABLE", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        cn.Open();

                        reader = cmd.ExecuteReader();

                        if (reader != null)
                        {
                            while (reader.Read())
                            {
                                ComprobantesEpsBM item = new ComprobantesEpsBM();
                                item.NID_PROC = reader["NID_PROC"] == DBNull.Value ? string.Empty : reader["NID_PROC"].ToString();
                                item.SBILLTYPE = reader["SBILLTYPE"] == DBNull.Value ? string.Empty : reader["SBILLTYPE"].ToString();
                                item.NINSUR_AREA = reader["NINSUR_AREA"] == DBNull.Value ? string.Empty : reader["NINSUR_AREA"].ToString();
                                item.NBILLNUM = reader["NBILLNUM"] == DBNull.Value ? string.Empty : reader["NBILLNUM"].ToString();
                                item.NRECEIPT = reader["NRECEIPT"] == DBNull.Value ? 0 : Convert.ToInt64(reader["NRECEIPT"]);
                                item.P_NID_COTIZACION = reader["NID_COTIZACION"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NID_COTIZACION"]);
                                item.NTIMES = reader["NTIMES"] == DBNull.Value ? 0 : int.Parse(reader["NTIMES"].ToString());
                                // Nota de Credito
                                item.SBILLTYPE_O = reader["SBILLTYPE_O"] == DBNull.Value ? string.Empty : reader["SBILLTYPE_O"].ToString();
                                item.NINSUR_AREA_O = reader["NINSUR_AREA_O"] == DBNull.Value ? string.Empty : reader["NINSUR_AREA_O"].ToString();
                                item.NBILLNUM_O = reader["NBILLNUM_O"] == DBNull.Value ? string.Empty : reader["NBILLNUM_O"].ToString();
                                response.Add(item);
                            }
                        }
                        cn.Close();
                    }
                    catch (Exception ex)
                    {
                        LogControl.save("GetJobsSendComprobantesEPS", ex.ToString(), "3");
                        response = new List<ComprobantesEpsBM>();
                    }
                    finally
                    {
                        if (cn.State == ConnectionState.Open) cn.Close();
                    }
                }
            }
            return response;
        }

        public void InsLogAuthEps(long cotizacion, string proceso, string url, string parametros, string resultados, string nid_proc = null)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["Conexion"].ConnectionString;

            try
            {
                using (OracleConnection cn = new OracleConnection(connectionString))
                using (OracleCommand cmd = new OracleCommand("INSUDB.INS_TBL_PD_LOG_AUTH", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_NID_COTIZACION", OracleDbType.Int64).Value = cotizacion;
                    cmd.Parameters.Add("P_SCOD_PROCESS", OracleDbType.Varchar2).Value = (object)proceso ?? DBNull.Value;
                    cmd.Parameters.Add("P_SURL", OracleDbType.Varchar2).Value = (object)url ?? DBNull.Value;
                    cmd.Parameters.Add("P_SJSON", OracleDbType.Varchar2).Value = (object)parametros ?? DBNull.Value;
                    cmd.Parameters.Add("P_SRESULT", OracleDbType.Varchar2).Value = (object)resultados ?? DBNull.Value;
                    cmd.Parameters.Add("P_NID_PROC", OracleDbType.Varchar2).Value = (object)nid_proc ?? DBNull.Value;

                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LogControl.save("InsLogAuthEps", $"Error BD InsLogAuthEps - COTIZACION: {cotizacion} | " + ex.ToString(), "3");
            }
        }

        public void UpdStatusComprobantesEPS(ComprobantesEpsBM request, int TIMES, int NSTATE)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["Conexion"].ConnectionString;

            try
            {
                using (OracleConnection cn = new OracleConnection(connectionString))
                using (OracleCommand cmd = new OracleCommand(GenericProcedures.pkg_Eps + ".UPD_STATUS_COMPROBANTES_EPS", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_NRECEIPT", OracleDbType.Varchar2).Value = request.NRECEIPT.ToString();
                    cmd.Parameters.Add("P_NTIMES", OracleDbType.Int32).Value = TIMES + 1;
                    cmd.Parameters.Add("P_NSTATE", OracleDbType.Int32).Value = NSTATE;

                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LogControl.save("UpdStatusComprobantesEPS", $"Error BD UpdStatus - NRECEIPT: {request?.NRECEIPT} | " + ex.ToString(), "3");
            }
        }


        public ErrorServiceVM ActualizarEstadoComprobantesEPS()
        {
            ErrorServiceVM response = new ErrorServiceVM
            {
                P_NCODE = "0",
                P_SMESSAGE = "OK"
            };

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.sp_ActualizarEstadoComprobante;
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros de salida del Procedure
                        var pCodErr = new OracleParameter("P_COD_ERR", OracleDbType.Int32, ParameterDirection.Output);
                        var pSMessage = new OracleParameter("P_SMESSAGE", OracleDbType.Varchar2, 4000, null, ParameterDirection.Output);

                        cmd.Parameters.Add(pCodErr);
                        cmd.Parameters.Add(pSMessage);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        response.P_NCODE = pCodErr.Value?.ToString() ?? "0";
                        response.P_SMESSAGE = pSMessage.Value?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        response.P_NCODE = "1";
                        response.P_SMESSAGE = ex.Message;
                        LogControl.save("ErrorEnvioPolizas - 900_PreLimpieza", response.P_SMESSAGE, "3");
                    }
                    finally
                    {
                        if (cn.State == ConnectionState.Open) cn.Close();
                    }
                }
            }

            return response;
        }


        /// <summary>
        /// Procesa la solicitud completa para guardar una póliza en la EPS.
        /// Delega la ejecución a EjecutarSavePolicyInterno manejando silenciosamente las excepciones fatales.
        /// </summary>
        public async Task<ErrorServiceVM> SendDataEPS(EPSJobVM policyJobVM)
        {
            var response = new ErrorServiceVM();
            if (policyJobVM == null || policyJobVM.NIDHEADERPROC <= 0)
            {
                return response;
            }

            try
            {
                var data = new EPSSavePolicyBM()
                {
                    P_NIDHEADERPROC = policyJobVM.NIDHEADERPROC.ToString()
                };

                response = await EjecutarSavePolicyInterno(data, null);
            }
            catch (Exception ex)
            {
                LogControl.save(LOG_TIMEOUT_ERR, $"Fallo Silencioso EPS - CABECERA: {policyJobVM?.NIDHEADERPROC} | Detalle: {ex.ToString()}", "3");
                InsLogAuthEps(0, "03 - Excepción Silenciosa SavePolicy", "WSKuntur" + "/PolicyManager/SavePolicyEPS", ex.Message, null, policyJobVM?.NIDHEADERPROC.ToString());
            }

            return response;
        }

        /// <summary>
        /// Construye la carga útil (Payload JSON), solicita autorización y transfiere el registro de la póliza a la EPS.
        /// Cambia los estados de la base de datos basándose en el éxito o fallo de la respuesta de red.
        /// </summary>
        private async Task<ErrorServiceVM> EjecutarSavePolicyInterno(EPSSavePolicyBM data, Action accionPostGuardado)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.P_NIDHEADERPROC))
            {
                return new ErrorServiceVM { P_NCODE = "1", P_SMESSAGE = "RequestProtecta es nulo o inválido" };
            }

            long idHeaderProc = Convert.ToInt64(data.P_NIDHEADERPROC);
            var dataSalud = getDataSalud(data);

            if (dataSalud.P_NCODE != "0" || dataSalud.dataList == null || !dataSalud.dataList.Any())
            {
                ActualizarEstadoTransacEps(idHeaderProc, 3);
                return new ErrorServiceVM { P_NCODE = "1", P_SMESSAGE = dataSalud.P_SMESSAGE ?? "No se encontró información de salud en BD" };
            }

            var item = dataSalud.dataList.First();
            var transaccion = MapTipoTransaccion(item.P_NTRANSAC);
            var dataEPS = ConstruirDataTransaccion(item);

            if (dataEPS == null)
            {
                ActualizarEstadoTransacEps(idHeaderProc, 3);
                return new ErrorServiceVM { P_NCODE = "1", P_SMESSAGE = "Hubo un error al construir la estructura JSON para la EPS" };
            }

            try
            {
                Response_EPS_Transaccion resultEPS;

                if (data.P_NRESEND == 0 || data.P_NRESEND == 2)
                {
                    resultEPS = await InvocarServicioEPS(dataEPS, transaccion);
                }
                else
                {
                    resultEPS = data.P_DATA_RESEND;
                }

                if (resultEPS != null)
                {
                    var responseBg = ProcesarRespuestaEPS(item, dataEPS, resultEPS, transaccion);
                    int estadoFinal = (responseBg.P_NCODE == "0") ? 2 : 3;

                    ActualizarEstadoTransacEps(idHeaderProc, estadoFinal);

                    if(estadoFinal == 2)
                    {
                        FinalizardocumentosEPS(idHeaderProc);
                    }

                    if (estadoFinal == 2)
                    {
                        try
                        {
                            FinalizardocumentosEPS(idHeaderProc);
                        }
                        catch (Exception exFin)
                        {
                            LogControl.save("FinalizardocumentosEPS", $"Fallo al finalizar docs (Cabecera: {idHeaderProc}) | {exFin.Message}", "3");
                        }

                        if (accionPostGuardado != null)
                        {
                            accionPostGuardado.Invoke();
                        }
                    }

                    return responseBg;
                }
                else
                {
                    ActualizarEstadoTransacEps(idHeaderProc, 3);
                    return new ErrorServiceVM { P_NCODE = "1", P_SMESSAGE = "No se obtuvo respuesta de la EPS" };
                }
            }
            catch (Exception ex)
            {
                ActualizarEstadoTransacEps(idHeaderProc, 3);
                LogControl.save("SavePolicyEPS - Excepción Fatal", ex.ToString(), "3");

                try
                {
                    string urlEmision = GetParamConfig("urlEmisionEPS_SCTR");
                    InsertLog(0, "03 - Excepción SavePolicyEPS", urlEmision, ex.Message, null, idHeaderProc.ToString());
                }
                catch { }

                return new ErrorServiceVM { P_NCODE = "1", P_SMESSAGE = "Error interno durante la transmisión: " + ex.Message };
            }
        }


        private ErrorServiceVM ProcesarRespuestaEPS(DataPolicyVM item, dataTransaccion dataEPS, Response_EPS_Transaccion resultEPS, string transaction)
        {
            if (!resultEPS.success)
            {
                insertErrorEPS(item.P_NID_COTIZACION, JsonConvert.SerializeObject(resultEPS), item.P_NID_PROC_EPS, item.P_NID_PROC);
                return new ErrorServiceVM { P_NCODE = "1", P_SMESSAGE = resultEPS.message };
            }

            try
            {
                long policyId = Convert.ToInt64(item.P_NPOLICY);
                string idProc = item.P_NCOT_MIXTA == "1" ? item.P_NID_PROC_EPS : item.P_NID_PROC;

                insert_policy_EPS(policyId, resultEPS.data.idContrato, idProc, $"SE REALIZO LA {transaction} CORRECTAMENTE");

                if (resultEPS.data.documentos != null && resultEPS.data.documentos.Any())
                {
                    int cotizacionId = Convert.ToInt32(item.P_NID_COTIZACION);
                    int transacId = Convert.ToInt32(item.P_NTRANSAC);

                    var documentos = resultEPS.data.documentos.Select(d => new Ins_Documentacion_EPS
                    {
                        P_NID_PROC_EPS = item.P_NID_PROC,
                        P_NBRANCH = 77,
                        P_NPRODUCT = 2,
                        P_NID_COTIZACION = cotizacionId,
                        P_NPOLICY = policyId,
                        P_ID_DOCUMENTO = d.idDocumento,
                        P_DES_DOCUMENTO = d.nombreDocumento,
                        P_NSTATE = 1,
                        P_MENSAJE_IMP = "TRABAJO INSERTADO",
                        P_NTYPE_TRANSAC = transacId
                    }).ToList();

                    Insert_Documentos_EPS(documentos);

                    var firstDoc = documentos.First();
                    UPD_TRX_CARGA_DEFINITIVA(firstDoc);
                    InsertarGeneracionDocumentosEps(firstDoc);
                }

                return new ErrorServiceVM { P_NCODE = "0", P_SMESSAGE = "Se realizó correctamente el envío a la EPS" };
            }
            catch (Exception ex)
            {
                LogControl.save("ProcesarRespuestaEPS", ex.ToString(), "3");
                insertErrorEPS(item.P_NID_COTIZACION, ex.Message, item.P_NID_PROC_EPS, item.P_NID_PROC);
                return new ErrorServiceVM { P_NCODE = "1", P_SMESSAGE = "Error al registrar la respuesta de la EPS en base de datos: " + ex.Message };
            }
        }

        public ErrorCode InsertarGeneracionDocumentosEps(Ins_Documentacion_EPS request)
        {
            var response = new ErrorCode();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.pkg_EPS + ".SP_INS_EPS_DOCUMENTOS_GENERAR";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NID_COTIZACION", OracleDbType.Int64, request.P_NID_COTIZACION, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NBRANCH", OracleDbType.Int64, request.P_NBRANCH, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NPRODUCT", OracleDbType.Int64, request.P_NPRODUCT, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_PROC", OracleDbType.Varchar2, request.P_NID_PROC_EPS?.ToString() ?? string.Empty, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NPOLICY", OracleDbType.Int64, request.P_NPOLICY, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NTYPETRANSAC", OracleDbType.Varchar2, request.P_NTYPE_TRANSAC.ToString(), ParameterDirection.Input);

                        var P_COD_ERR = new OracleParameter("P_COD_ERR", OracleDbType.Int64, ParameterDirection.Output) { Size = 200 };
                        var P_MESSAGE = new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, ParameterDirection.Output) { Size = 4000 };

                        cmd.Parameters.Add(P_COD_ERR);
                        cmd.Parameters.Add(P_MESSAGE);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        response.P_COD_ERR = P_COD_ERR.Value == DBNull.Value ? 0 : Convert.ToInt32(P_COD_ERR.Value.ToString());
                        response.P_MESSAGE = P_MESSAGE.Value?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        response.P_COD_ERR = 1;
                        response.P_MESSAGE = ex.Message;
                        LogControl.save("InsertarGeneracionDocumentosEps", ex.ToString(), "3");
                    }
                    finally
                    {
                        if (cn.State == ConnectionState.Open) cn.Close();
                    }
                }
            }

            return response;
        }

        public SalidadPolizaEmit UPD_TRX_CARGA_DEFINITIVA(Ins_Documentacion_EPS request)
        {
            var response = new SalidadPolizaEmit();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.pkg_EPS + ".UPD_TRX_CARGA_DEFINITIVA_EPS";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NBRANCH", OracleDbType.Int64, request.P_NBRANCH, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NPRODUCT", OracleDbType.Int64, request.P_NPRODUCT, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_COTIZACION", OracleDbType.Int64, request.P_NID_COTIZACION, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NPOLICY", OracleDbType.Int64, request.P_NPOLICY, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NTYPE_TRANSAC", OracleDbType.Int64, request.P_NTYPE_TRANSAC, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_PROC", OracleDbType.Varchar2, request.P_NID_PROC_EPS, ParameterDirection.Input);

                        var P_COD_ERR = new OracleParameter("P_NCODE", OracleDbType.Int32, ParameterDirection.Output) { Size = 200 };
                        var P_MESSAGE = new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, ParameterDirection.Output) { Size = 4000 };

                        cmd.Parameters.Add(P_COD_ERR);
                        cmd.Parameters.Add(P_MESSAGE);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        response.P_COD_ERR = P_COD_ERR.Value == DBNull.Value ? 0 : Convert.ToInt32(P_COD_ERR.Value.ToString());
                        response.P_MESSAGE = P_MESSAGE.Value?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        response.P_COD_ERR = 1;
                        response.P_MESSAGE = ex.Message;
                        LogControl.save("UPD_TRX_CARGA_DEFINITIVA", ex.ToString(), "3");
                    }
                    finally
                    {
                        if (cn.State == ConnectionState.Open) cn.Close();
                    }
                }
            }

            return response;
        }

        public poliza_eps_sctr Ins_Documentos_EPS(Ins_Documentacion_EPS insDocEPSList)
        {
            var response = new poliza_eps_sctr();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.pkg_EPS + ".SP_INS_DOCUMENTO_EPS";
                        cmd.CommandType = CommandType.StoredProcedure;

                        string docNameSeguro = string.IsNullOrEmpty(insDocEPSList.P_DES_DOCUMENTO)
                            ? string.Empty
                            : (insDocEPSList.P_DES_DOCUMENTO.Length > 100 ? insDocEPSList.P_DES_DOCUMENTO.Substring(0, 100) : insDocEPSList.P_DES_DOCUMENTO);

                        cmd.Parameters.Add("P_NID_PROC_EPS", OracleDbType.Varchar2, insDocEPSList.P_NID_PROC_EPS, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NBRANCH", OracleDbType.Int64, insDocEPSList.P_NBRANCH, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NPRODUCT", OracleDbType.Int64, insDocEPSList.P_NPRODUCT, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_COTIZACION", OracleDbType.Int64, insDocEPSList.P_NID_COTIZACION, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NPOLICY", OracleDbType.Int64, insDocEPSList.P_NPOLICY, ParameterDirection.Input);
                        cmd.Parameters.Add("P_ID_DOCUMENTO", OracleDbType.Int64, insDocEPSList.P_ID_DOCUMENTO, ParameterDirection.Input);
                        cmd.Parameters.Add("P_DES_DOCUMENTO", OracleDbType.Varchar2, docNameSeguro, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NSTATE", OracleDbType.Int64, insDocEPSList.P_NSTATE, ParameterDirection.Input);
                        cmd.Parameters.Add("P_MENSAJE_IMP", OracleDbType.Varchar2, insDocEPSList.P_MENSAJE_IMP, ParameterDirection.Input);

                        var P_NCODE = new OracleParameter("P_NCODE", OracleDbType.Int32, null, ParameterDirection.Output) { Size = 100 };
                        var P_MESSAGE = new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, null, ParameterDirection.Output) { Size = 4000 };

                        cmd.Parameters.Add(P_NCODE);
                        cmd.Parameters.Add(P_MESSAGE);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        response.P_NCODE = P_NCODE.Value == DBNull.Value ? 0 : Convert.ToInt32(P_NCODE.Value.ToString());
                        response.P_MESSAGE = P_MESSAGE.Value?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        response.P_NCODE = 1;
                        response.P_MESSAGE = ex.ToString();
                        LogControl.save("Ins_Documentos_EPS", ex.ToString(), "3");
                    }
                }
            }

            return response;
        }

        private void Insert_Documentos_EPS(List<Ins_Documentacion_EPS> insDocEPSList)
        {
            if (insDocEPSList == null || !insDocEPSList.Any()) return;

            foreach (var docEPS in insDocEPSList)
            {
                Ins_Documentos_EPS(docEPS);
            }
        }

        public ErrorCode insertErrorEPS(string cotizacion, string json, string nid_proc_epc, string nid_proc_sctr)
        {
            var response = new ErrorCode();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.pkg_EPS + ".SP_UDP_JSON_RELANZAR_EPS";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NID_COTIZACION", OracleDbType.Int32, cotizacion, ParameterDirection.Input);
                        cmd.Parameters.Add("P_SJSON", OracleDbType.Clob, json, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_PROC_EPS", OracleDbType.Varchar2, nid_proc_epc, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_PROC_SCTR", OracleDbType.Varchar2, nid_proc_sctr, ParameterDirection.Input);

                        var P_NCODE = new OracleParameter("P_COD_ERR", OracleDbType.Int32, null, ParameterDirection.Output) { Size = 100 };
                        var P_SMESSAGE = new OracleParameter("P_MESSAGE", OracleDbType.Varchar2, null, ParameterDirection.Output) { Size = 4000 };

                        cmd.Parameters.Add(P_NCODE);
                        cmd.Parameters.Add(P_SMESSAGE);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        response.P_COD_ERR = P_NCODE.Value == DBNull.Value ? 0 : Convert.ToInt32(P_NCODE.Value.ToString());
                        response.P_MESSAGE = P_SMESSAGE.Value?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        response.P_COD_ERR = 1;
                        response.P_MESSAGE = ex.Message;
                        LogControl.save("insertErrorEPS", ex.ToString(), "3");
                    }
                    finally
                    {
                        if (cn.State == ConnectionState.Open) cn.Close();
                    }
                }
            }

            return response;
        }

        public int insert_policy_EPS(long poliza_sctr, long poliza_eps, string nid_proc_eps, string mensaje)
        {
            int result = 0;
            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.pkg_Eps + ".SP_UDP_POLIZA_EPS";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NPOLICY_SCTR", OracleDbType.Int64, poliza_sctr, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NPOLICY_EPS", OracleDbType.Int64, poliza_eps, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_PROC_EPS", OracleDbType.Varchar2, nid_proc_eps, ParameterDirection.Input);
                        cmd.Parameters.Add("P_MENSAJE", OracleDbType.Varchar2, mensaje, ParameterDirection.Input);

                        var P_COD_ERR = new OracleParameter("P_NCODE", OracleDbType.Int64, null, ParameterDirection.Output) { Size = 200 };
                        cmd.Parameters.Add(P_COD_ERR);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        if (P_COD_ERR.Value != null && P_COD_ERR.Value != DBNull.Value)
                        {
                            result = Convert.ToInt32(P_COD_ERR.Value.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        result = 1;
                        LogControl.save("insert_policy_EPS", ex.ToString(), "3");
                    }
                    finally
                    {
                        if (cn.State == ConnectionState.Open) cn.Close();
                    }
                }
            }

            return result;
        }


        private async Task<Response_EPS_Transaccion> InvocarServicioEPS(dataTransaccion dataEPS, string transaccion)
        {
            string uriEmision = GetParamConfig("urlEmisionEPS_SCTR");
            int nidCotizacion = Convert.ToInt32(dataEPS.codigoCotizacion);

            InsertLog(nidCotizacion, $"01 - SE ENVIA JSON A EPS - {transaccion}", uriEmision, JsonConvert.SerializeObject(dataEPS), null, dataEPS.codigoProceso);

            return await invocarServicio_EPS_TRA(dataEPS.codigoCotizacion, JsonConvert.SerializeObject(dataEPS), transaccion, dataEPS.codigoProceso);
        }

        /// <summary>
        /// Gestiona operaciones variadas de la EPS según el tipo solicitado.
        /// Retorna metadatos, datos serializados o resultados de validación.
        /// </summary>
        public RelanzarEPSVM GetManagementEPS(RelanzarDocumentoVM data, int tipo)
        {
            var response = new RelanzarEPSVM();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.sp_RelanzarEPS;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NIDHEADERPROC", OracleDbType.Int64, data.nidheaderproc, ParameterDirection.Input);
                        cmd.Parameters.Add("P_TIPO", OracleDbType.Int32, tipo, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NUSERCODE", OracleDbType.Int32, data.suser, ParameterDirection.Input);

                        var P_NCODE = new OracleParameter("P_COD_ERR", OracleDbType.Int32, null, ParameterDirection.Output) { Size = 100 };
                        var P_SMESSAGE = new OracleParameter("P_CP_SMESSAGEOD_ERR", OracleDbType.Varchar2, null, ParameterDirection.Output) { Size = 4000 };
                        var P_SJSON = new OracleParameter("P_SJSON", OracleDbType.Clob, null, ParameterDirection.Output);
                        var P_NTYPE_TRANSAC = new OracleParameter("P_NTYPE_TRANSAC", OracleDbType.Int32, null, ParameterDirection.Output);
                        var C_TABLE = new OracleParameter("C_TABLE", OracleDbType.RefCursor, null, ParameterDirection.Output);

                        cmd.Parameters.Add(P_NCODE);
                        cmd.Parameters.Add(P_SMESSAGE);
                        cmd.Parameters.Add(P_SJSON);
                        cmd.Parameters.Add(P_NTYPE_TRANSAC);
                        cmd.Parameters.Add(C_TABLE);

                        cn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            response.P_NCODE = P_NCODE.Value == DBNull.Value ? 0 : Convert.ToInt32(P_NCODE.Value.ToString());
                            response.P_SMESSAGE = P_SMESSAGE.Value?.ToString() ?? string.Empty;
                            response.P_SJSON = (P_SJSON.Value is OracleClob clob && !clob.IsNull) ? clob.Value : string.Empty;
                            response.P_NTYPE_TRANSAC = P_NTYPE_TRANSAC.Value == DBNull.Value ? 0 : Convert.ToInt32(P_NTYPE_TRANSAC.Value.ToString());

                            if (tipo == 3 && reader != null)
                            {
                                var tableData = new List<DetalleTransacEPS>();
                                while (reader.Read())
                                {
                                    tableData.Add(new DetalleTransacEPS
                                    {
                                        norder = reader["NORDER"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NORDER"]),
                                        nid_cotizacion = reader["NID_COTIZACION"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NID_COTIZACION"]),
                                        nidheaderproc = reader["NIDHEADERPROC"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NIDHEADERPROC"]),
                                        nid_proc = reader["NID_PROC"] == DBNull.Value ? string.Empty : reader["NID_PROC"].ToString(),
                                        dcompdate = reader["DCOMPDATE"] == DBNull.Value ? string.Empty : reader["DCOMPDATE"].ToString(),
                                        sstate = reader["SSTATE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SSTATE"]),
                                        message = reader["MESSAGE"] == DBNull.Value ? string.Empty : reader["MESSAGE"].ToString(),
                                        nusercode = reader["NUSERCODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NUSERCODE"]),
                                        suser = reader["SUSER"] == DBNull.Value ? string.Empty : reader["SUSER"].ToString()
                                    });
                                }
                                response.TableData = tableData;
                            }
                        }

                        if (!string.IsNullOrEmpty(response.P_SJSON))
                        {
                            try
                            {
                                if (tipo == 4)
                                    response.dataListCotizacion = JsonConvert.DeserializeObject<dataQuotation_EPS>(response.P_SJSON);
                                else if (tipo == 2)
                                    response.JsonData = JsonConvert.DeserializeObject<Response_EPS_Transaccion>(response.P_SJSON);
                            }
                            catch (Exception exJson)
                            {
                                response.P_NCODE = 3;
                                response.P_SMESSAGE = $"Error al deserializar JSON: {exJson.Message}";
                                LogControl.save("GetManagementEPS - JSON", exJson.ToString(), "3");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        response.P_NCODE = 1;
                        response.P_SMESSAGE = ex.Message;
                        LogControl.save("GetManagementEPS", ex.ToString(), "3");
                    }
                }
            }

            return response;
        }


        public DataPolicyEPSVM getDataSalud(EPSSavePolicyBM data)
        {
            var response = new DataPolicyEPSVM();
            string connString = ConfigurationManager.ConnectionStrings["Conexion"].ConnectionString;

            using (var connection = new OracleConnection(connString))
            using (var command = new OracleCommand("REA_DATA_SCTR_EPS_SEND", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("P_NIDHEADERPROC", OracleDbType.Int32).Value = data.P_NIDHEADERPROC;
                command.Parameters.Add("P_NRESEND", OracleDbType.Int32).Value = data.P_NRESEND;
                command.Parameters.Add("P_NCODE", OracleDbType.Int32).Direction = ParameterDirection.Output;
                command.Parameters.Add("P_SMESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                command.Parameters.Add("C_TABLE", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                    response.P_NCODE = command.Parameters["P_NCODE"].Value?.ToString() ?? "1";
                    response.P_SMESSAGE = command.Parameters["P_SMESSAGE"].Value?.ToString() ?? string.Empty;

                    if (response.P_NCODE == "0")
                    {
                        var refCursor = command.Parameters["C_TABLE"].Value as OracleRefCursor;
                        if (refCursor != null && !refCursor.IsNull)
                        {
                            using (var reader = refCursor.GetDataReader())
                            {
                                while (reader.Read())
                                {
                                    var item = new DataPolicyVM
                                    {
                                        P_NID_COTIZACION = reader["NID_COTIZACION"]?.ToString() ?? string.Empty,
                                        P_NID_PROC = reader["NID_PROC"]?.ToString() ?? string.Empty,
                                        P_NPOLICY = reader["NPOLICY"]?.ToString() ?? string.Empty,
                                        P_FECHA_PAGO = reader["FECHA_PAGO"]?.ToString() ?? string.Empty,
                                        P_DSTARTDATE_ASE = FormatearFechaReader(reader["DSTARTDATE_ASE"]),
                                        P_DEXPIRDAT_ASE = FormatearFechaReader(reader["DEXPIRDAT_ASE"]),
                                        P_DEFFECDATE = FormatearFechaReader(reader["DEFFECDATE"]),
                                        P_DEXPIRDAT = FormatearFechaReader(reader["DEXPIRDAT"]),
                                        P_SDELIMITER = reader["DELIMITACION"]?.ToString() ?? string.Empty,
                                        P_NCURRENCY = reader["NCURRENCY"]?.ToString() ?? string.Empty,
                                        P_NPAYFREQ = reader["NPAYFREQ"]?.ToString() ?? string.Empty,
                                        P_NTIP_RENOV = reader["NTIP_RENOV"]?.ToString() ?? string.Empty,
                                        P_FORMA_PAGO = reader["FORMA_PAGO"]?.ToString() ?? string.Empty,
                                        P_FACT_ANT = reader["FACT_ANT"]?.ToString() ?? string.Empty,
                                        P_FACT_MES_VENC = reader["FACT_MES_VENC"]?.ToString() ?? string.Empty,
                                        P_NTRANSAC = reader["NTYPE_TRANSAC"]?.ToString() ?? string.Empty,
                                        P_FECHA_TRANSACCION = FormatearFechaReader(reader["FECHA_TRANSACCION"]),
                                        P_NUSERCODE = reader["NUSER_CODE"]?.ToString() ?? string.Empty,
                                        P_NPREM_MINIMA = reader["NPREMIUM_MIN_AU"]?.ToString() ?? "0",
                                        P_NAMO_AFEC = reader["NAMO_AFEC"]?.ToString() ?? "0",
                                        P_NIVA = reader["NIVA"]?.ToString() ?? "0",
                                        P_NDE = reader["NDE"]?.ToString() ?? "0",
                                        P_NAMOUNT = reader["NAMOUNT"]?.ToString() ?? "0",
                                        P_SCOD_CIP = reader["COD_CIP"]?.ToString() ?? string.Empty,
                                        P_SLOCATION = reader["SSEDE"]?.ToString() ?? string.Empty,
                                        P_NCOT_MIXTA = reader["NCOT_MIXTA"]?.ToString() ?? string.Empty,
                                        P_NID_PROC_EPS = reader["NID_PROC_EPS"]?.ToString() ?? string.Empty,
                                        P_TIPO_FACT = reader["TIPO_FACT"]?.ToString() ?? string.Empty
                                    };
                                    response.dataList.Add(item);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    response.P_NCODE = "1";
                    response.P_SMESSAGE = "Error al obtener datos de Oracle: " + ex.Message;
                    LogControl.save("getDataSalud", ex.ToString(), "3");
                }
            }

            return response;
        }

        private string FormatearFechaReader(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value) return string.Empty;
            return DateTime.TryParse(dbValue.ToString(), out DateTime date) ? date.ToString("dd/MM/yyyy") : dbValue.ToString();
        }


        public void ActualizarEstadoTransacEps(long idHeaderProc, int estado)
        {
            try
            {
                string connString = System.Configuration.ConfigurationManager.ConnectionStrings["Conexion"].ConnectionString;
                using (var connection = new OracleConnection(connString))
                using (var command = new OracleCommand("UPDATE TBL_PD_COT_TRANSAC SET TRANSAC_EPS = :p_estado WHERE NIDHEADERPROC = :p_cabecera", connection))
                {
                    command.Parameters.Add("p_estado", OracleDbType.Int32).Value = estado;
                    command.Parameters.Add("p_cabecera", OracleDbType.Int64).Value = idHeaderProc;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LogControl.save("ActualizarEstadoTransacEps", $"No se pudo cambiar el estado a {estado} para la cabecera {idHeaderProc}. Detalle: " + ex.Message, "3");
            }
        }


        private string MapTipoTransaccion(string tipoTransaccion)
        {
            switch (tipoTransaccion)
            {
                case "1": return "EMISION";
                case "2": return "INCLUSION";
                case "3": return "EXCLUSION";
                case "4": return "RENOVACION";
                case "5": return "NETEO";
                case "6": return "REVERSO";
                case "7": return "ANULACIÓN";
                case "8": return "ENDOSO";
                case "9": return "GEN. FACTURACION";
                case "11": return "DECLARACION";
                default: return "EMISION";
            }
        }

        /// <summary>
        /// Mapea la información recuperada de BD hacia la estructura de la solicitud EPS final.
        /// Contiene conversiones de tipos y formatos de fecha esenciales.
        /// </summary>
        private dataTransaccion ConstruirDataTransaccion(DataPolicyVM item)
        {
            var dataEPS = new dataTransaccion();
            var act_fact = GetParamConfig("ACT_FACTURACION");

            try
            {
                dataEPS.codigoCotizacion = item.P_NID_COTIZACION;
                dataEPS.codigoProceso = item.P_NID_PROC.ToString();
                dataEPS.codigoContrato = item.P_NPOLICY;
                dataEPS.fechaVencimientoPago = GetFechaPago(item.P_NID_COTIZACION);
                dataEPS.fechaEfectoAseguradoRecibo = FormatearFecha(item.P_DSTARTDATE_ASE);
                dataEPS.fechaExpiracionAseguradoRecibo = FormatearFecha(item.P_DEXPIRDAT_ASE);
                dataEPS.fechaEfectoPoliza = FormatearFecha(item.P_DEFFECDATE);
                dataEPS.fechaExpiracionPoliza = FormatearFecha(item.P_DEXPIRDAT);
                dataEPS.asignacionActividadAltoRiesgo = item.P_SDELIMITER == "1";
                dataEPS.codigoMoneda = item.P_NCURRENCY;
                dataEPS.codigoFrecuenciaPago = item.P_NTIP_RENOV.ToString();
                dataEPS.codigoFrecuenciaRenovacion = item.P_NPAYFREQ.ToString();
                dataEPS.codigoFormaPago = item.P_FORMA_PAGO;
                dataEPS.asignacionFacturacionAnticipada = item.P_FACT_ANT == "1";
                dataEPS.asignacionRegulaMesVencido = item.P_FACT_MES_VENC == "1";
                dataEPS.tipoFacturacionRMV = (act_fact == "1" && !string.IsNullOrEmpty(item.P_TIPO_FACT)) ? Convert.ToInt32(item.P_TIPO_FACT) : (dataEPS.asignacionRegulaMesVencido ? 1 : 0);
                dataEPS.codigoTipoTransaccion = item.P_NTRANSAC;
                dataEPS.fechaTransaccion = FormatearFecha(item.P_FECHA_TRANSACCION);
                dataEPS.codigoUsuarioRegistro = item.P_NUSERCODE;
                dataEPS.primaMinimaAutorizada = decimal.Parse(item.P_NPREM_MINIMA);
                dataEPS.primaComercial = Convert.ToDecimal(item.P_NAMO_AFEC);
                dataEPS.igv = Convert.ToDecimal(item.P_NIVA);
                dataEPS.derechoEmision = new string[] { "5" }.Contains(item.P_NTRANSAC) ? 0 : Convert.ToDecimal(item.P_NDE);
                dataEPS.primaTotal = Convert.ToDecimal(item.P_NAMOUNT);
                dataEPS.cipPagoEfectivo = item.P_SCOD_CIP;
                dataEPS.sede = item.P_SLOCATION;
                dataEPS.riesgos = !new string[] { "6", "7" }.Contains(item.P_NTRANSAC) ? GetRiesgos(item.P_NID_COTIZACION, item.P_NID_PROC) : null;
                dataEPS.asegurados = !new string[] { "6", "7" }.Contains(item.P_NTRANSAC) ? GetAsegurados(item.P_NID_PROC) : null;
            }
            catch (Exception ex)
            {
                LogControl.save("ConstruirDataTransaccion", ex.ToString(), "3");
                return null;
            }

            return dataEPS;
        }

        public string GetFechaPago(string nid_cotizacion)
        {
            string fechaPago = null;

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.pkg_EPS + ".SP_GET_FECHA_PAGO_DIAS_CREDITO";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NID_COTIZACION", OracleDbType.Int32, nid_cotizacion, ParameterDirection.Input);

                        var P_FECHA_PAGO = new OracleParameter("P_FECHA_PAGO", OracleDbType.Varchar2, 200, null, ParameterDirection.Output);
                        cmd.Parameters.Add(P_FECHA_PAGO);

                        cn.Open();

                        cmd.ExecuteNonQuery();

                        var valOutput = P_FECHA_PAGO.Value?.ToString();
                        if (!string.IsNullOrEmpty(valOutput) && valOutput != "null")
                        {
                            string[] formatosPermitidos = new string[]
                            {
                                "dd/MM/yyyy",
                                "dd/MM/yyyy HH:mm:ss",
                                "dd/MM/yyyy hh:mm:ss tt",
                                "yyyy-MM-dd",
                                "yyyy-MM-dd HH:mm:ss"
                            };

                            if (DateTime.TryParseExact(valOutput.Trim(), formatosPermitidos, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                            {
                                fechaPago = fecha.ToString("yyyy-MM-dd");
                            }
                            else if (DateTime.TryParse(valOutput, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaRespaldo))
                            {
                                fechaPago = fechaRespaldo.ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                fechaPago = valOutput.Split(' ')[0];
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogControl.save("GetFechaPago", ex.ToString(), "3");
                    }
                }
            }

            return fechaPago;
        }

        private string FormatearFecha(string fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha)) return string.Empty;

            if (DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaParseada))
            {
                return fechaParseada.ToString("yyyy-MM-dd");
            }

            return fecha;
        }

        /// <summary>
        /// Obtiene la lista de asegurados desde la base de datos para un proceso específico.
        /// Utiliza un bloque 'using' para asegurar la liberación del cursor en Oracle.
        /// </summary>
        public List<asegurados> GetAsegurados(string nid_proc)
        {
            List<asegurados> resultPackage = new List<asegurados>();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.pkg_EPS + ".SP_GET_ASEGURADOS";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NID_PROC_EPS", OracleDbType.Varchar2, nid_proc, ParameterDirection.Input);

                        var C_TABLE = new OracleParameter("C_TABLE", OracleDbType.RefCursor, ParameterDirection.Output);
                        cmd.Parameters.Add(C_TABLE);

                        cn.Open();

                        using (var odr = cmd.ExecuteReader())
                        {
                            var tableData = new List<DetalleTransacEPS>();
                            while (odr.Read())
                            {
                                asegurados item = new asegurados();
                                item.nombres = odr["SFIRSTNAME"] == DBNull.Value ? "" : odr["SFIRSTNAME"].ToString();
                                item.apellidoPaterno = odr["SLASTNAME"] == DBNull.Value ? "" : odr["SLASTNAME"].ToString();
                                item.apellidoMaterno = odr["SLASTNAME2"] == DBNull.Value ? "" : odr["SLASTNAME2"].ToString();
                                item.codigoPlan = odr["NMODULEC"] == DBNull.Value ? "" : odr["NMODULEC"].ToString();
                                item.codigoTipoDocumento = odr["NIDDOC_TYPE"] == DBNull.Value ? "" : odr["NIDDOC_TYPE"].ToString();
                                item.numeroDocumento = odr["SIDDOC"] == DBNull.Value ? "" : odr["SIDDOC"].ToString();
                                item.fechaNacimiento = odr["DBIRTHDAT"] == DBNull.Value ? "" : DateTime.Parse(odr["DBIRTHDAT"].ToString()).ToString("yyyy-MM-dd");
                                item.remuneracion = odr["NREMUNERACION"] == DBNull.Value ? 0 : Convert.ToDecimal(odr["NREMUNERACION"]);
                                item.codigoUnicoCliente = odr["SCLIENT"] == DBNull.Value ? "" : odr["SCLIENT"].ToString();
                                item.primaCobrada = odr["NPREMIUMN"] == DBNull.Value ? 0 : Convert.ToDecimal(odr["NPREMIUMN"]);
                                item.igvCobrado = odr["NIGV"] == DBNull.Value ? 0 : Convert.ToDecimal(odr["NIGV"]);
                                item.derechoEmisionCobrado = odr["NDE"] == DBNull.Value ? 0 : Convert.ToDecimal(odr["NDE"]);
                                item.primaBrutaCobrada = odr["NPREMIUM"] == DBNull.Value ? 0 : Convert.ToDecimal(odr["NPREMIUM"]);
                                item.codigoGenero = odr["SSEXCLIEN"] == DBNull.Value ? "" : odr["SSEXCLIEN"].ToString() == "3" ? "2" : odr["SSEXCLIEN"].ToString();
                                item.correoElectronico = odr["SE_MAIL"] == DBNull.Value ? "" : odr["SE_MAIL"].ToString();
                                item.codigoTipoTelefono = odr["NPHONE_TYPE"] == DBNull.Value ? "" : odr["NPHONE_TYPE"].ToString();
                                item.telefono = odr["SPHONE"] == DBNull.Value ? "" : odr["SPHONE"].ToString();
                                item.codigoNeteo = odr["NCOD_NETEO"] == DBNull.Value ? "" : odr["NCOD_NETEO"].ToString();
                                resultPackage.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogControl.save("GetAsegurados", ex.ToString(), "3");
                    }
                }
            }

            return resultPackage;
        }

        public List<riesgos> GetRiesgos(string cod_cotizacion, string nid_proc)
        {
            List<riesgos> resultPackage = new List<riesgos>();

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.pkg_EPS + ".SP_GET_RIESGOS";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NID_COTIZACION", OracleDbType.Varchar2, cod_cotizacion, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_PROC", OracleDbType.Varchar2, nid_proc, ParameterDirection.Input);

                        var C_TABLE = new OracleParameter("C_TABLE", OracleDbType.RefCursor, ParameterDirection.Output);
                        cmd.Parameters.Add(C_TABLE);

                        cn.Open();

                        using (var odr = cmd.ExecuteReader())
                        {
                            while (odr.Read())
                            {
                                int trabajadores = odr["NNUM_TRABAJADORES"] == DBNull.Value ? 0 : Convert.ToInt32(odr["NNUM_TRABAJADORES"]);

                                if (trabajadores != 0)
                                {
                                    var item = new riesgos
                                    {
                                        codigoProducto = odr["NPRODUCT"]?.ToString() ?? string.Empty,
                                        codigoPlan = odr["NMODULEC"]?.ToString() ?? string.Empty,
                                        codigoCategoria = odr["NMODULEC"]?.ToString() ?? string.Empty,
                                        cantidadTrabajador = trabajadores,
                                        planillaTotal = odr["NSUM_PREMIUM"] == DBNull.Value ? 0m : Convert.ToDecimal(odr["NSUM_PREMIUM"]),
                                        tasaAutorizada = odr["NTASA_AUTOR"] == DBNull.Value ? 0m : Convert.ToDecimal(odr["NTASA_AUTOR"])
                                    };
                                    resultPackage.Add(item);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogControl.save("GetRiesgos", ex.ToString(), "3");
                    }
                }
            }

            return resultPackage;
        }

        public async Task<AuthEPSResult> generarTokenEPS(string nid_cotizacion, string transaccion, string nid_proc)
        {
            AuthEPSResult response = null;

            string urlToken = GetParamConfig("urlTokenEPS_SCTR");
            string epsUser = GetParamConfig("EPSUser");
            string epsPwd = GetParamConfig("EPSoPwd");

            var credentials = new
            {
                usuario = epsUser,
                clave = epsPwd
            };

            var jsonCredentials = JsonConvert.SerializeObject(credentials);

            try
            {
                using (var content = new StringContent(jsonCredentials, Encoding.UTF8, "application/json"))
                using (var resServices = await _httpClient.PostAsync(urlToken, content))
                {
                    var resultToken = await resServices.Content.ReadAsStringAsync();

                    if (!resServices.IsSuccessStatusCode || string.IsNullOrEmpty(resultToken))
                    {
                        LogControl.save("invocarServicio_EPS - API EMI", resultToken, "3");
                        InsertLog(Convert.ToInt64(nid_cotizacion), "02 - RESPUESTA  TOKEN EPS [EPS ERROR] - " + transaccion, urlToken, "No se genero Token del servicio EPS. Detalle: " + resultToken, null, nid_proc);
                    }
                    else
                    {
                        response = JsonConvert.DeserializeObject<AuthEPSResult>(resultToken);
                    }
                }
            }
            catch (Exception ex)
            {
                LogControl.save("invocarServicio_EPS - API EMI Token", ex.ToString(), "3");
                InsertLog(Convert.ToInt64(nid_cotizacion), "02 - RESPUESTA  TOKEN EPS [EPS ERROR] - " + transaccion, urlToken, ex.Message, null, nid_proc);
            }

            return response;
        }


        public async Task<Response_EPS_Transaccion> invocarServicio_EPS_TRA(string cotizacion, string json, string transaccion, string nid_proc)
        {
            var result = new Response_EPS_Transaccion();
            var nid_cotizacion = Convert.ToInt32(cotizacion);

            try
            {
                var resultToken = await generarTokenEPS(cotizacion, transaccion, nid_proc);

                if (resultToken != null)
                {
                    result = await generarEnvioDataEPS(resultToken, json, transaccion, nid_proc, cotizacion);
                }
            }
            catch (Exception ex)
            {
                result = null;
                LogControl.save("invocarServicio_EPS - API EMI", ex.ToString(), "3");
                var urlEmisionEPS_SCTR = GetParamConfig("urlEmisionEPS_SCTR");
                InsertLog(nid_cotizacion, "02 - RESPUESTAS EPS [MG ERROR] - " + transaccion, urlEmisionEPS_SCTR, ex.ToString(), null, nid_proc);
            }
            finally
            {
                // _semaphore.Release();
            }

            return result;
        }

        public void InsertLog(long cotizacion, string proceso, string url, string parametros, string resultados, string nid_proc = null)
        {
            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    proceso = string.IsNullOrEmpty(proceso) ? string.Empty : (proceso.Length > 200 ? proceso.Substring(0, 200) : proceso);
                    url = string.IsNullOrEmpty(url) ? string.Empty : (url.Length > 4000 ? url.Substring(0, 4000) : url);
                    resultados = string.IsNullOrEmpty(resultados) ? string.Empty : (resultados.Length > 4000 ? resultados.Substring(0, 4000) : resultados);
                    nid_proc = string.IsNullOrEmpty(nid_proc) ? string.Empty : (nid_proc.Length > 30 ? nid_proc.Substring(0, 30) : nid_proc);
                    parametros = string.IsNullOrEmpty(parametros) ? string.Empty : parametros;

                    try
                    {
                        cmd.Connection = cn;
                        cmd.CommandText = GenericProcedures.sp_InsertarLogAuth;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NID_COTIZACION", OracleDbType.Int64, cotizacion, ParameterDirection.Input);
                        cmd.Parameters.Add("P_SCOD_PROCESS", OracleDbType.Varchar2, proceso, ParameterDirection.Input);
                        cmd.Parameters.Add("P_SURL", OracleDbType.Varchar2, url, ParameterDirection.Input);
                        cmd.Parameters.Add("P_SJSON", OracleDbType.Clob, parametros, ParameterDirection.Input);
                        cmd.Parameters.Add("P_SRESULT", OracleDbType.Varchar2, resultados, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NID_PROC", OracleDbType.Varchar2, nid_proc, ParameterDirection.Input);

                        cn.Open();

                        // CORRECCIÓN: Un SP que hace INSERT debe ejecutarse con ExecuteNonQuery, no ExecuteReader.
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        LogControl.save("InsertLog", ex.ToString(), "3");
                    }
                }
            }
        }

        /// <summary>
        /// Envía una solicitud HTTP POST asíncrona hacia la API de la EPS.
        /// </summary>
        /// <param name="resultToken">Token Bearer de autenticación previamente validado.</param>
        /// <param name="json">Carga útil serializada a transferir.</param>
        /// <param name="transaccion">Nombre de la operación para trazabilidad en logs.</param>
        /// <param name="nid_proc">Código del proceso.</param>
        /// <param name="cotizacion">Identificador de la cotización para relacionar el log.</param>
        /// <returns>El objeto Response_EPS_Transaccion deserializado, o null si la conexión falla.</returns>
        private async Task<Response_EPS_Transaccion> generarEnvioDataEPS(AuthEPSResult resultToken, string json, string transaccion, string nid_proc, string cotizacion)
        {
            Response_EPS_Transaccion response = null;
            string urlEmision = GetParamConfig("urlEmisionEPS_SCTR");

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, urlEmision))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", resultToken.token);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (var resServices = await _httpClient.SendAsync(request).ConfigureAwait(false))
                    {
                        var responseEPS = await resServices.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(responseEPS))
                        {
                            response = JsonConvert.DeserializeObject<Response_EPS_Transaccion>(responseEPS);

                            if (response != null && response.success)
                            {
                                string urlToken = GetParamConfig("urlTokenEPS_SCTR");
                                InsertLog(Convert.ToInt64(cotizacion), "02 - RESPUESTA EPS [EPS OK] - " + transaccion, urlEmision, responseEPS, null, nid_proc);
                                LogControl.save("generarTransaccionEPS", "{\"cotizacion\": \"" + cotizacion + "\", \"mensaje\":  \"02 - Se recibe respuesta de EPS - Transaccion de Poliza\", \"url\": \"" + urlToken + "\", \"json\": " + responseEPS + " }", "2", "EPS");
                            }
                            else
                            {
                                string msg = response != null ? response.message : "Error desconocido en EPS";
                                LogControl.save("invocarServicio_EPS - API EMI", msg, "3");
                                InsertLog(Convert.ToInt64(cotizacion), "02 - RESPUESTA EPS [MG ERROR] - " + transaccion, urlEmision, responseEPS, null, nid_proc);
                            }
                        }
                        else
                        {
                            InsertLog(Convert.ToInt64(cotizacion), "02 - RESPUESTA EPS [EPS ERROR] - " + transaccion, urlEmision, "Respuesta vacía del servidor", null, nid_proc);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogControl.save("invocarServicio_EPS - API EMI Envio", ex.ToString(), "3");
                InsertLog(Convert.ToInt64(cotizacion), "02 - RESPUESTA EPS [MG ERROR] - " + transaccion, urlEmision, ex.Message, null, nid_proc);
            }

            return response;
        }

        /// <summary>
        /// Ejecuta el proceso de limpieza y actualización de estados para comprobantes atascados.
        /// Retorna a estado 1 aquellos que pasen la validación del log.
        /// </summary>
        public ErrorServiceVM RestaurarEstadoComprobantesEps()
        {
            var response = new ErrorServiceVM
            {
                P_NCODE = "0",
                P_SMESSAGE = "OK"
            };

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand("SP_UPD_ESTADO_COMPROBANTES_EPS", cn))
                {
                    try
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var pCodErr = new OracleParameter("P_COD_ERR", OracleDbType.Int32, ParameterDirection.Output);
                        var pSMessage = new OracleParameter("P_SMESSAGE", OracleDbType.Varchar2, 4000, null, ParameterDirection.Output);

                        cmd.Parameters.Add(pCodErr);
                        cmd.Parameters.Add(pSMessage);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        response.P_NCODE = pCodErr.Value?.ToString() ?? "0";
                        response.P_SMESSAGE = pSMessage.Value?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        response.P_NCODE = "1";
                        response.P_SMESSAGE = ex.Message;
                        LogControl.save("RestaurarEstadoComprobantesEps", ex.ToString(), "3");
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Ejecuta el proceso de relanzamiento de EPS específicamente para evaluar concurrencia 
        /// y liberar pólizas atascadas en RAM/CPU (P_TIPO = 9).
        /// </summary>
        public ErrorServiceVM LiberarPolizasEpsAtascadas()
        {
            var response = new ErrorServiceVM
            {
                P_NCODE = "0",
                P_SMESSAGE = "OK"
            };

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand(GenericProcedures.sp_RelanzarEPS, cn))
                {
                    try
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NIDHEADERPROC", OracleDbType.Int64, DBNull.Value, ParameterDirection.Input);
                        cmd.Parameters.Add("P_TIPO", OracleDbType.Int32, 9, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NUSERCODE", OracleDbType.Int32, DBNull.Value, ParameterDirection.Input);

                        // Parámetros de Salida obligatorios por la firma del SP
                        var pCodErr = new OracleParameter("P_COD_ERR", OracleDbType.Int32, null, ParameterDirection.Output) { Size = 100 };
                        var pSMessage = new OracleParameter("P_SMESSAGE", OracleDbType.Varchar2, null, ParameterDirection.Output) { Size = 4000 };
                        var pSJson = new OracleParameter("P_SJSON", OracleDbType.Clob, null, ParameterDirection.Output);
                        var pNTypeTransac = new OracleParameter("P_NTYPE_TRANSAC", OracleDbType.Int32, null, ParameterDirection.Output);
                        var cTable = new OracleParameter("C_TABLE", OracleDbType.RefCursor, null, ParameterDirection.Output);

                        cmd.Parameters.Add(pCodErr);
                        cmd.Parameters.Add(pSMessage);
                        cmd.Parameters.Add(pSJson);
                        cmd.Parameters.Add(pNTypeTransac);
                        cmd.Parameters.Add(cTable);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        response.P_NCODE = pCodErr.Value == DBNull.Value ? "0" : pCodErr.Value.ToString();
                        response.P_SMESSAGE = pSMessage.Value?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        response.P_NCODE = "1";
                        response.P_SMESSAGE = ex.Message;
                        LogControl.save("LiberarPolizasEpsAtascadas", ex.ToString(), "3");
                    }
                }
            }

            return response;
        }

        public ErrorServiceVM FinalizardocumentosEPS(long nidheaderproc)
        {
            var response = new ErrorServiceVM
            {
                P_NCODE = "0",
                P_SMESSAGE = "OK"
            };

            using (OracleConnection cn = new OracleConnection(ConfigurationManager.ConnectionStrings["Conexion"].ToString()))
            {
                using (OracleCommand cmd = new OracleCommand(GenericProcedures.sp_RelanzarEPS, cn))
                {
                    try
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("P_NIDHEADERPROC", OracleDbType.Int64, nidheaderproc, ParameterDirection.Input);
                        cmd.Parameters.Add("P_TIPO", OracleDbType.Int32, 10, ParameterDirection.Input);
                        cmd.Parameters.Add("P_NUSERCODE", OracleDbType.Int32, DBNull.Value, ParameterDirection.Input);

                        // Parámetros de Salida obligatorios por la firma del SP
                        var pCodErr = new OracleParameter("P_COD_ERR", OracleDbType.Int32, null, ParameterDirection.Output) { Size = 100 };
                        var pSMessage = new OracleParameter("P_SMESSAGE", OracleDbType.Varchar2, null, ParameterDirection.Output) { Size = 4000 };
                        var pSJson = new OracleParameter("P_SJSON", OracleDbType.Clob, null, ParameterDirection.Output);
                        var pNTypeTransac = new OracleParameter("P_NTYPE_TRANSAC", OracleDbType.Int32, null, ParameterDirection.Output);
                        var cTable = new OracleParameter("C_TABLE", OracleDbType.RefCursor, null, ParameterDirection.Output);

                        cmd.Parameters.Add(pCodErr);
                        cmd.Parameters.Add(pSMessage);
                        cmd.Parameters.Add(pSJson);
                        cmd.Parameters.Add(pNTypeTransac);
                        cmd.Parameters.Add(cTable);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        response.P_NCODE = pCodErr.Value == DBNull.Value ? "0" : pCodErr.Value.ToString();
                        response.P_SMESSAGE = pSMessage.Value?.ToString() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        response.P_NCODE = "1";
                        response.P_SMESSAGE = ex.Message;
                        LogControl.save("LiberarPolizasEpsAtascadas", ex.ToString(), "3");
                    }
                }
            }

            return response;
        }
    }
}
