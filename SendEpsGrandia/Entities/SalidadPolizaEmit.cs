using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Entities
{
    public class SalidadPolizaEmit
    {
        public Int64 P_NPOLICY { get; set; }
        public Int32 P_COD_ERR { get; set; }
        public string P_MESSAGE { get; set; }
        public Int64 P_POL_SALUD { get; set; }
        public Int64 P_POL_PENSION { get; set; }
        public Int64 P_POL_VLEY { get; set; }//MARC
        public Int64 P_POL_AP { get; set; }//MARC
        public Int64 P_NCONSTANCIA { get; set; }
        public Int64 P_POL_COMPANY { get; set; } // JDD
        public Int64 P_POL_COVID { get; set; }//MARC
        public string P_NPOLICY_ { get; set; }
        public string P_NCONSTANCIA_ { get; set; }
        public List<PolicyReceipt> receipts { get; set; }
        public Int64 P_FLAG_REVERSO { get; set; } // CASUÍSTICA RMV
    }

    public class PolicyReceipt
    {
        public Int64 nBranch { get; set; }
        public Int64 nProduct { get; set; }
        public long nReceipt { get; set; }
        public string dEffecDate { get; set; }
        public string dIssueDat { get; set; }
        public decimal nPremium { get; set; }
    }
}
