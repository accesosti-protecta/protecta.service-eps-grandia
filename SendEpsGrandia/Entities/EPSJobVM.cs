using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Entities
{
    public class EPSJobVM
    {
        public dynamic NIDHEADERPROC { get; set; }
        public int NRESEND { get; set; } = 0;
    }

    public class ErrorCode
    {
        public Int32 P_COD_ERR { get; set; }
        public string P_MESSAGE { get; set; }
        public Int32 P_ORDER { get; set; }
    }

}
