using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Entities
{
    public class PrintResponseVM
    {
        public int NCODE { get; set; }
        public string SMESSAGE { get; set; }
        public string PATH_PDF { get; set; }
        public string PATH_PDF_CERTIF { get; set; }
        public string SCONDICIONADO { get; set; }
        public string SRESULTADO_PRINT { get; set; }
        public int NGEN_CD { get; set; }
        public string PATH_GEN { get; set; }
        public int RESULT_SEND { get; set; }
        public bool SEND_PE { get; set; } = false;
        public dynamic NPERIOD { get; set; } //ED RENTA
    }

    public class ResponseGraph
    {
        public int codError { get; set; }
        public string message { get; set; }
        public GenericResponse data { get; set; }
        public List<Error> errors { get; set; }
    }

    public class GenericResponse
    {
        public string referenceURL { get; set; }
    }

    public class Error
    {
        public List<string> path { get; set; }
        public string errorType { get; set; }
        public List<Location> locations { get; set; }
        public string message { get; set; }
    }

    public class Location
    {
        public int line { get; set; }
        public int column { get; set; }
        public string sourceName { get; set; }
    }

    public class ErrorServiceVM
    {
        public string P_NCODE { get; set; }
        public string P_SMESSAGE { get; set; }
        public int P_RESPUESTA { get; set; }
        public string P_DTABLE { get; set; }
    }
    public class EPSSavePolicyBM
    {
        public string P_NIDHEADERPROC { get; set; }
        public int P_NRESEND { get; set; } = 0;
        public Response_EPS_Transaccion P_DATA_RESEND { get; set; }
    }

    public class Response_EPS_Transaccion
    {
        public bool success { get; set; }
        public string message { get; set; }
        public Data_EPS data { get; set; }
    }

    public class Data_EPS
    {
        public int idContrato { get; set; }
        public string codigoContrato { get; set; }
        public int idCotizacion { get; set; }
        public List<Document_EPS> documentos { get; set; }
    }

    public class Document_EPS
    {
        public int idDocumento { get; set; }
        public string nombreDocumento { get; set; }
    }
}
