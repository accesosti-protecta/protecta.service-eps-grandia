using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Helpers
{
    public class ELog
    {
        public static string obtainConfig(string value)
        {
            try
            {
                string configValue = System.Configuration.ConfigurationManager.AppSettings[value];
                return configValue;
            }
            catch
            {
                return null;
            }
        }
    }


    public class LogControl
    {
        private static readonly object _fileLock = new object();
        /// <summary>
        /// Se guarda log de forma detallada, 
        /// se coloca el campo de tipo log con el fin de tener detalle de este (1: seguimiento (Comentarios) / 2: json (request / response) / 3: error)
        /// </summary>
        public static void save(string method, string message, string typeLog, string provider = "PD") // 1: seguimiento / 2: json / 3: error
        {
            try
            {
                typeLog = typeLog == "1" ? "2" : typeLog;
                string fecha = System.DateTime.Now.ToString("yyyyMMdd");
                string hora = System.DateTime.Now.ToString("HH:mm:ss:ffff");

                string path = string.Empty;

                try
                {
                    path = System.Configuration.ConfigurationManager.AppSettings["pathLog" + typeLog];
                }
                catch
                {
                }

                if (string.IsNullOrEmpty(path))
                {
                    switch (typeLog)
                    {
                        case "1": path = @"D:\log\SendEpsGrandia\qa\seguimiento\"; break;
                        case "2": path = @"D:\log\SendEpsGrandia\qa\json\"; break;
                        case "3": path = @"D:\log\SendEpsGrandia\qa\error\"; break;
                        default: path = @"D:\log\SendEpsGrandia\"; break;
                    }
                }

                path = Path.Combine(path, fecha);

                provider = string.IsNullOrEmpty(provider) ? "PD" : provider;

                string pathName = Path.Combine(path, method + ".txt");

                lock (_fileLock)
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    using (StreamWriter sw = new StreamWriter(pathName, true))
                    {
                        sw.WriteLine(method + " - " + hora);
                        sw.WriteLine(message);
                        sw.WriteLine(new string('=', 85));
                        sw.WriteLine("");
                        sw.Flush();
                    }
                }
            }
            catch
            {
                // Silencioso
            }
        }
    }

}
