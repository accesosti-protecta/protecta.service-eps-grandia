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
        public static string GetValueConfig(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
