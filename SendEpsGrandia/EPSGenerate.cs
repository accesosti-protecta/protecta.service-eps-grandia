using System;
using System.ComponentModel;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using System.Configuration;
using SendEpsGrandia.Util;
using SendEpsGrandia.Helpers;
using System.Threading.Tasks;

namespace SendEpsGrandia
{
    public partial class EPSGenerate : ServiceBase
    {
        public EPSGenerate()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            string msg = $"FATAL CRASH (UnhandledException): {ex.Message}\nStack Trace: {ex.StackTrace}";
            LogError(msg);

            if (e.IsTerminating)
            {
                Environment.Exit(1);
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogError($"FATAL CRASH (UnobservedTaskException): {e.Exception.Message}\nStack Trace: {e.Exception.StackTrace}");
            e.SetObserved();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                ConfigureSendDataEPSJob(new ElapsedEventHandler(this.OnTimerExecuteSendDataEPSJobProcess));
                ConfigureSendComprobantesEPS(new ElapsedEventHandler(this.OnTimerExecuteSendComprobantesEPSJobPrintProcess));
                ConfigureRestaurarEstadosComprobanteEPS(new ElapsedEventHandler(this.OnTimerExecuteRestaurarEstadosComprobanteEPSProcess));
                ConfigureLiberarPolizasEPS(new ElapsedEventHandler(this.OnTimerExecuteLiberarPolizasEPSProcess));
            }
            catch (Exception ex)
            {
                LogError($"Error crítico en OnStart: {ex}");
            }
        }

        protected override void OnStop()
        {
        }

        private void ConfigureSendDataEPSJob(ElapsedEventHandler method)
        {
            double interval = Convert.ToDouble(ConfigurationManager.AppSettings["IntervalSendDataEPSJob"]);
            Timer timer = new Timer { Interval = interval };
            timer.Elapsed += method;
            timer.Start();
        }

        private void ConfigureSendComprobantesEPS(ElapsedEventHandler method)
        {
            double interval = Convert.ToDouble(ConfigurationManager.AppSettings["IntervalSendComprobantesEPS"]);
            Timer timer = new Timer { Interval = interval };
            timer.Elapsed += method;
            timer.Start();
        }

        private void ConfigureRestaurarEstadosComprobanteEPS(ElapsedEventHandler method)
        {
            double interval = Convert.ToDouble(ConfigurationManager.AppSettings["IntervalRestaurarEstadosComprobanteEPS"]);
            Timer timer = new Timer { Interval = interval };
            timer.Elapsed += method;
            timer.Start();
        }

        private void ConfigureLiberarPolizasEPS(ElapsedEventHandler method)
        {
            double interval = Convert.ToDouble(ConfigurationManager.AppSettings["IntervalLiberarPolizasEPS"]);
            Timer timer = new Timer { Interval = interval };
            timer.Elapsed += method;
            timer.Start();
        }

        public void OnTimerExecuteSendDataEPSJobProcess(object sender, ElapsedEventArgs args)
        {
            try
            {
                if (!SendDataEPSJob.IsBusy) SendDataEPSJob.RunWorkerAsync();
            }
            catch (Exception ex) { LogError($"Error Timer SendDataEPS: {ex}"); }
        }

        public void OnTimerExecuteSendComprobantesEPSJobPrintProcess(object sender, ElapsedEventArgs args)
        {
            try
            {
                if (!SendComprobantesEPSJob.IsBusy) SendComprobantesEPSJob.RunWorkerAsync();
            }
            catch (Exception ex) { LogError($"Error Timer ComprobantesEPS: {ex}"); }
        }

        public void OnTimerExecuteRestaurarEstadosComprobanteEPSProcess(object sender, ElapsedEventArgs args)
        {
            try
            {
                if (!RestaurarEstadosComprobanteEPS.IsBusy) RestaurarEstadosComprobanteEPS.RunWorkerAsync();
            }
            catch (Exception ex) { LogError($"Error Timer RestaurarEstados: {ex}"); }
        }

        public void OnTimerExecuteLiberarPolizasEPSProcess(object sender, ElapsedEventArgs args)
        {
            try
            {
                if (!LiberarPolizasEPS.IsBusy) LiberarPolizasEPS.RunWorkerAsync();
            }
            catch (Exception ex) { LogError($"Error Timer LiberarPolizas: {ex}"); }
        }


        private void SendDataEPSJob_DoWork(object sender, DoWorkEventArgs e)
        {
            new SendDataEPSJobProcess().ExecuteProcess();
        }

        private void SendComprobantesEPSJob_DoWork(object sender, DoWorkEventArgs e)
        {
            new SendComprobantesEpsProcess().ExecuteProcess();
        }

        private void RestaurarEstadosComprobanteEPS_DoWork(object sender, DoWorkEventArgs e)
        {
            new RestaurarEstadosComprobantesEPS().ExecuteProcess();
        }

        private void LiberarPolizasEPS_DoWork(object sender, DoWorkEventArgs e)
        {
            new LiberarPolizasEPS().ExecuteProcess();
        }

        private static void LogError(string message)
        {
            try
            {
                LogControl.save("ManejadorExcepciones", message, "3");
                System.Diagnostics.EventLog.WriteEntry("SendEPSGrandia", message, System.Diagnostics.EventLogEntryType.Error);

                string logFilePath = @"D:\log\SendEpsGrandia\FATAL_CRASH\SendEPSGrandia_Fatal.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}{new string('-', 50)}{Environment.NewLine}");
            }
            catch
            {
                // Silencioso total
            }
        }
    }
}