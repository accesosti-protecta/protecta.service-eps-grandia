using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Entities
{
    public class ValBrokerModel
    {
        public string SCLIENT { get; set; }
        public int COD_ERROR { get; set; }
        public string P_SMESSAGE { get; set; }
    }

    public class RelanzarDocumentoVM
    {
        public Int64 npolicy { get; set; }
        public int nbranch { get; set; }
        public int nproduct { get; set; }
        public Int64 nidheaderproc { get; set; }
        public string sruta { get; set; }
        public List<string> srutalist { get; set; }
        public int suser { get; set; }
    }

    public class ObtListEPS
    {
        public int nidheaderproc { get; set; }
        public int tipo { get; set; }
        public int suser { get; set; }
    }

    public class RelanzarEPSVM
    {
        public int P_NCODE { get; set; }
        public string P_SMESSAGE { get; set; }
        public string P_SJSON { get; set; }
        public int P_NTYPE_TRANSAC { get; set; }
        public int P_NUSERCODE { get; set; }
        public List<DetalleTransacEPS> TableData { get; set; }
        public dataQuotation_EPS dataListCotizacion { get; set; }
        public Response_EPS_Transaccion JsonData { get; set; }

    }

    public class DetalleTransacEPS
    {
        public int norder { get; set; }
        public int nid_cotizacion { get; set; }
        public int nidheaderproc { get; set; }
        public string nid_proc { get; set; }
        public string dcompdate { get; set; }
        public int sstate { get; set; }
        public string message { get; set; }
        public int nusercode { get; set; }
        public string suser { get; set; }
    }

    public class dataQuotation_EPS //AVS - INTERCONEXION SABSA
    {

        public string codigoCotizacion { get; set; }
        public int codigoRamo { get; set; }
        public string codigoProceso { get; set; }
        public string codigoContrato { get; set; }
        public string fechaEfecto { get; set; }
        public string fechaExpiracion { get; set; }
        public string fechaRegistro { get; set; }
        public string codigoActividadTecnica { get; set; }
        public string codigoSubActividadTecnica { get; set; }
        public string ubigeo { get; set; }
        public int primaMinima { get; set; }
        public int primaMinimaPropuesta { get; set; }
        public bool asignacionActividadAltoRiesgo { get; set; }
        public string codigoMoneda { get; set; }
        public string codigoEstadoCotizacion { get; set; }
        public string codigoUsuarioRegistro { get; set; }
        public double primaComercial { get; set; }
        public double igv { get; set; }
        public double derechoEmision { get; set; }
        public double primaTotal { get; set; }
        public string comentario { get; set; }
        public List<contratanteBM> contratante { get; set; }
        public List<intermediariosBM> intermediarios { get; set; }
        public List<riesgosBM> riesgos { get; set; }

    }

    public class riesgosBM
    {
        public string codigoProducto { get; set; }
        public string codigoPlan { get; set; }
        public string codigoCategoria { get; set; }
        public int cantidadTrabajador { get; set; }
        public double planillaTotal { get; set; }
        public double tasaCalculada { get; set; }
        public double tasaPropuesta { get; set; }
        public double primaMensualAutorizada { get; set; }
    }

    public class intermediariosBM
    {

        public string codigoExterno { get; set; }
        public string codigoTipoDocumento { get; set; }
        public string numeroDocumento { get; set; }
        public string nombreCompleto { get; set; }
        public string nombres { get; set; }
        public string apellidoPaterno { get; set; }
        public string apellidoMaterno { get; set; }
        public string codigoTipoCorredor { get; set; }
        public double gastoAsesoria { get; set; }
        public double gastoAsesoriaPropuesto { get; set; }
        public string ubigeo { get; set; }
        public string direccion { get; set; }
        public string correoElectronico { get; set; }
        public string telefono { get; set; }

    }

    public class contratanteBM  //AVS  INTERCONEXION SABSA
    {

        public string codigoTipoDocumento { get; set; }
        public string numeroDocumento { get; set; }
        public string nombreCompleto { get; set; }
        public string nombres { get; set; }
        public string apellidoPaterno { get; set; }
        public string apellidoMaterno { get; set; }
        public string correoElectronico { get; set; }
        public string telefono { get; set; }
        public string direccion { get; set; }
        public string ubigeo { get; set; }
        public string sede { get; set; }
    }
}
