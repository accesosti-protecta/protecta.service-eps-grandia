using SendEpsGrandia.Entities;
using SendEpsGrandia.Repositories;
using SendEpsGrandia.Helpers;
using System;

namespace SendEpsGrandia
{
    public class LiberarPolizasEPS
    {
        /// <summary>
        /// Método principal del proceso.
        /// Llama directamente a la capa de datos (EPSPrintDA) de forma síncrona y óptima,
        /// liberando las pólizas que se quedaron atascadas en RAM/CPU.
        /// </summary>
        public ErrorServiceVM ExecuteProcess()
        {
            try
            {
                EPSPrintDA epsDA = new EPSPrintDA();
                return epsDA.LiberarPolizasEpsAtascadas();
            }
            catch (Exception ex)
            {
                LogControl.save("LiberarPolizasEPS_Job", "Error al invocar la capa de datos: " + ex.ToString(), "3");

                return new ErrorServiceVM
                {
                    P_NCODE = "1",
                    P_SMESSAGE = "Error interno en el proceso de liberación de pólizas: " + ex.Message
                };
            }
        }
    }
}