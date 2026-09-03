using SendEpsGrandia.Entities;
using SendEpsGrandia.Helpers;
using SendEpsGrandia.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SendEpsGrandia
{
    public class RelanzarEPSJobComprobante
    {
        private readonly EPSPrintDA epsDA = new EPSPrintDA();

        public ErrorServiceVM ExecuteProcess()
        {
            try
            {
                List<EPSJobVM> jobsList = epsDA.GetManagementEPS(11);

                if (jobsList == null || !jobsList.Any())
                {
                    return new ErrorServiceVM
                    {
                        P_NCODE = "0",
                        P_SMESSAGE = "No existen registros pendientes por procesar."
                    };
                }

                ParallelOptions parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 5
                };

                Parallel.ForEach(jobsList, parallelOptions, job =>
                {
                    try
                    {
                        job.NRESEND = 2;
                        ErrorServiceVM result = PolicyGenerate(job).GetAwaiter().GetResult();

                        if (result != null && result.P_NCODE != "0")
                        {
                            LogControl.save("ExecuteProcess_Item", "Error en NIDHEADERPROC " + job.NIDHEADERPROC + ": " + result.P_SMESSAGE, "3");
                        }
                    }
                    catch (Exception exTask)
                    {
                        LogControl.save("ExecuteProcess_Task", "Error en NIDHEADERPROC " + job.NIDHEADERPROC + ": " + exTask.ToString(), "3");
                    }
                });

                return new ErrorServiceVM
                {
                    P_NCODE = "0",
                    P_SMESSAGE = "Proceso de relanzamiento finalizado correctamente para " + jobsList.Count + " registros."
                };
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

        public Task<ErrorServiceVM> PolicyGenerate(EPSJobVM job)
        {
            return SendDataEps(job);
        }

        public Task<ErrorServiceVM> SendDataEps(EPSJobVM job)
        {
            return epsDA.SendDataEPS(job, 1);
        }
    }
}