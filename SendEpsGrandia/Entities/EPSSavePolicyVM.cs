using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Entities
{
    public class EPSSavePolicyVM
    {

    }

    public class EPSRelanzarMasivo
    {
        public int? P_TIPO { get; set; }
        public string P_FECHA { get; set; }
        public int? P_NID_COTIZACION { get; set; }
    }

    public class DataPolicyEPSVM : ErrorServiceVM
    {
        public List<DataPolicyVM> dataList { get; set; } = new List<DataPolicyVM>();
    }

    public class DataPolicyVM
    {
        public string P_NID_COTIZACION { get; set; }
        public string P_NID_PROC { get; set; }
        public string P_NPOLICY { get; set; }
        public string P_DEFFECDATE { get; set; }
        public string P_DEXPIRDAT { get; set; }
        public string P_DSTARTDATE_POL { get; set; }
        public string P_DEXPIRDAT_POL { get; set; }
        public string P_SDELIMITER { get; set; }
        public string P_NCURRENCY { get; set; }
        public string P_NTIP_RENOV { get; set; }
        public string P_NPAYFREQ { get; set; }
        public string P_FORMA_PAGO { get; set; }
        public string P_FACT_ANT { get; set; }
        public string P_FACT_MES_VENC { get; set; }
        public string P_NTRANSAC { get; set; }
        public string P_NUSERCODE { get; set; }
        public string P_NPREM_MINIMA { get; set; }
        public string P_NAMO_AFEC { get; set; }
        public string P_NIVA { get; set; }
        public string P_NDE { get; set; }
        public string P_NAMOUNT { get; set; }
        public string P_SLOCATION { get; set; }
        public string P_FECHA_PAGO { get; set; }
        public string P_DSTARTDATE_ASE { get; set; }
        public string P_DEXPIRDAT_ASE { get; set; }
        public string P_FECHA_TRANSACCION { get; set; }
        public string P_SCOD_CIP { get; set; }
        public string P_NCOT_MIXTA { get; set; }
        public string P_NID_PROC_EPS { get; set; }
        public string P_TIPO_FACT { get; set; } //SCTR-1347
    }
}
