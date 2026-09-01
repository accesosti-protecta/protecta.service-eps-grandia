using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Entities
{
    public class ComprobantesEpsBM
    {
        public int P_NID_COTIZACION { get; set; }
        public int P_NBRANCH { get; set; }
        public int P_NPRODUCT { get; set; }
        public Int64 P_NPOLICY { get; set; }
        public int P_NSTATE { get; set; }
        public string P_NID_PROC { get; set; }
        public string P_NTYPE_TRANSAC { get; set; }
        public int P_COD_ERR { get; set; }
        public string P_MESSAGE { get; set; }
        public string SBILLTYPE { get; set; }
        public string NINSUR_AREA { get; set; }
        public Int64 NRECEIPT { get; set; }
        public string NID_PROC { get; set; }
        public string SSTATE { get; set; }
        public string NBILLNUM { get; set; }
        public int NCODE { get; set; }
        public int NTIMES { get; set; }
        public string SMESSAGE { get; set; }

        // Nota de credito
        public string SBILLTYPE_O { get; set; }
        public string NINSUR_AREA_O { get; set; }
        public string NBILLNUM_O { get; set; }
    }

    public class ErrorCodeComprobantesEps
    {
        public Int32 P_COD_ERR { get; set; }
        public string P_MESSAGE { get; set; }
        public Int32 P_ORDER { get; set; }
    }

    public class AuthEPSResult
    {
        public string success { get; set; }
        public string message { get; set; }
        public string token { get; set; }
    }

}
