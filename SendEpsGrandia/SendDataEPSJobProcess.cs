using Newtonsoft.Json;
using SendEpsGrandia.Entities;
using SendEpsGrandia.Helpers;
using SendEpsGrandia.Repositories;
using SendEpsGrandia.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SendEpsGrandia
{
    /// <summary>
    /// Procesa el envío de trabajos EPS en segundo plano de manera asíncrona.
    /// Garantiza la limitación estricta de hilos en paralelo mediante SemaphoreSlim.
    /// </summary>
    public class SendDataEPSJobProcess : GenericMethods
    {
        private static SemaphoreSlim _semaphoreEps;
        private static int _currentMaxLimit = 0;
        private static readonly object _lockObj = new object();
        private readonly EPSPrintDA _epsPrintDA = new EPSPrintDA();

        /// <summary>
        /// Método principal de ejecución del Job.
        /// Obtiene el límite máximo de hilos desde la base de datos y calcula los espacios disponibles.
        /// Extrae estrictamente la cantidad de trabajos que el sistema puede soportar en el momento actual
        /// y delega su ejecución al ThreadPool de manera controlada.
        /// </summary>
        public void ExecuteProcess()
        {
            string paramHilos = _epsPrintDA.GetParamConfig("HILOS_POLIZA_EPS");
            int maxLimit = (int.TryParse(paramHilos, out int val) && val > 0) ? val : 5;

            // Se Inicializa segura con Double-Check Locking
            if (_semaphoreEps == null)
            {
                lock (_lockObj)
                {
                    if (_semaphoreEps == null)
                    {
                        _semaphoreEps = new SemaphoreSlim(maxLimit, maxLimit);
                    }
                }
            }

            // 1. Consultar únicamente la cantidad disponible para evitar bloqueos
            int slotsDisponibles = _semaphoreEps.CurrentCount;

            if (slotsDisponibles <= 0)
                return;

            // 2. Traer de la base de datos exactamente los registros que podemos atender AHORA
            var jobsList = _epsPrintDA.GetJobList(slotsDisponibles);

            if (jobsList == null || !jobsList.Any())
                return;

            foreach (var job in jobsList)
            {
                // 3. Se usa WaitAsync(0) o se intenta adquirir el slot dentro de la tarea.
                if (!_semaphoreEps.Wait(0))
                    break;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await PolicyGenerate(job);
                    }
                    catch (Exception ex)
                    {
                        LogControl.save("ExecuteProcess_Task", ex.ToString(), "3");
                    }
                    finally
                    {
                        // 4. Se libera el slot SIEMPRE para que el siguiente Timer o ciclo tome el hilo libre
                        _semaphoreEps.Release();
                    }
                });
            }
        }

        public Task<ErrorServiceVM> PolicyGenerate(EPSJobVM job)
        {
            return SendDataEps(job);
        }


        public Task<ErrorServiceVM> SendDataEps(EPSJobVM job)
        {
            return _epsPrintDA.SendDataEPS(job);
        }
    }
}