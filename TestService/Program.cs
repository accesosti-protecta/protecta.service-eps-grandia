using System;
using SendEpsGrandia; // Namespace del proyecto principal

namespace TestService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("       INICIO DE PRUEBAS - SEND EPS GRANDIA       ");
            Console.WriteLine("==================================================");

            try
            {
                var wsProcess = new EPSGenerate();

                // -----------------------------------------------------------------
                // Descomenta ÚNICAMENTE el servicio que quieras probar manualmente:
                // -----------------------------------------------------------------

                // Option A: Probar invocando el manejador del Timer de EPSGenerate
                // wsProcess.OnTimerExecuteSendDataEPSJobProcess(null, null);
                // wsProcess.OnTimerExecuteSendComprobantesEPSJobPrintProcess(null, null);
                // wsProcess.OnTimerExecuteRestaurarEstadosComprobanteEPSProcess(null, null);
                // wsProcess.OnTimerExecuteLiberarPolizasEPSProcess(null, null);

                // Opción B: Probar el nuevo relanzamiento de comprobantes
                Console.WriteLine("Ejecutando RelanzarEPSJobComprobante...");
                var relanzarJob = new RelanzarEPSJobComprobante();
                var resultado = relanzarJob.ExecuteProcess();

                Console.WriteLine($"[RESULTADO] Código: {resultado?.P_NCODE} | Mensaje: {resultado?.P_SMESSAGE}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR EXCEPCION] {ex}");
            }

            Console.WriteLine("==================================================");
            Console.WriteLine("  PROCESO FINALIZADO - Presione ENTER para salir  ");
            Console.WriteLine("==================================================");
            Console.ReadLine();
        }
    }
}