using SendEpsGrandia.Entities;
using SendEpsGrandia.Repositories;
using SendEpsGrandia.Helpers;
using System;

namespace SendEpsGrandia
{
    public class RestaurarEstadosComprobantesEPS
    {
        /// <summary>
        /// Método principal del proceso.
        /// Llama directamente a la capa de datos (EPSPrintDA) sin crear hilos adicionales,
        /// manteniendo la ejecución rápida y suave para el servidor IIS.
        /// </summary>
        public ErrorServiceVM ExecuteProcess()
        {
            try
            {
                EPSPrintDA epsDA = new EPSPrintDA();
                return epsDA.RestaurarEstadoComprobantesEps();
            }
            catch (Exception ex)
            {
                LogControl.save("RestaurarEstadosComprobantesEPS_Job", "Error al invocar la capa de datos: " + ex.ToString(), "3");

                return new ErrorServiceVM
                {
                    P_NCODE = "1",
                    P_SMESSAGE = "Error interno en el proceso de restauración: " + ex.Message
                };
            }
        }
    }
}