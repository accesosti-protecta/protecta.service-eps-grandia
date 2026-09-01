using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Util
{
    public class GenericMethods
    {
        private const string alphabet = "abcdefghijklmnopqrstuvwxyz";
        private int count;
        private int correlativeNumber;
        private static readonly object _fileLock = new object();
        private const string LOG_TIMEOUT_ERR = "ErrorAnexoEPS - 902_TimeoutRed";
        private const string LOG_FORMATO_ERR = "ErrorAnexoEPS - 903_FormatoHtml";
        private const string LOG_NAS_ERR = "ErrorAnexoEPS - 905_EscrituraNAS";
        private const string LOG_BASE64_ERR = "ErrorAnexoEPS - 906_Base64Corrupto";
        private const string LOG_GLOBAL_ANEXO_ERR = "ErrorAnexoEPS - 999_ExcepcionGeneral";

        public static string GetValueConfig(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
