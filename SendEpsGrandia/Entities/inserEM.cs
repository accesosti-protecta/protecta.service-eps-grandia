using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Entities
{
    public class dataQuotation
    {
        public string NID_COTIZACION { get; set; }
        public string NPRODUCT { get; set; }
        public string NMODULEC { get; set; }
        public string NNUM_TRABAJADORES { get; set; }
        public double? NMONTO_PLANILLA { get; set; }
        public double? NTASA_CALCULADA { get; set; }
        public double? NTASA_PROP { get; set; }
        public double? NTASA_AUTOR { get; set; }
        public string NPREMIUM_MIN { get; set; }
        public string NPREMIUM_MIN_PR { get; set; }
        public string NPREMIUM_MIN_AU { get; set; }
        public string NPREMIUM_END { get; set; }
        public string NSUM_PREMIUMN { get; set; }
        public double? NSUM_IGV { get; set; }
        public double? NSUM_PREMIUM { get; set; }
        public int? NUSERCODE { get; set; }
        public double? NRATE { get; set; }
        public string NDISCOUNT { get; set; }
        public string NACTIVITYVARIATION { get; set; }
        public string SSTATREGT { get; set; }
        public string NMODULEC_FINAL { get; set; }
        public string NAMO_AFEC { get; set; }
        public string NIVA { get; set; }
        public string NAMOUNT { get; set; }
        public string NDE { get; set; }
        public int? FRECUENCIA_PAGO { get; set; }
        public string NID_PROC { get; set; }
        public int? NPAYFREQ { get; set; }
        public string P_TIPO_COT { get; set; }
    }

    public class dataTransaccion
    {
        public string codigoCotizacion { get; set; }
        public string codigoProceso { get; set; }
        public string codigoContrato { get; set; }
        public string fechaVencimientoPago { get; set; }
        public string fechaEfectoAseguradoRecibo { get; set; }
        public string fechaExpiracionAseguradoRecibo { get; set; }
        public string fechaEfectoPoliza { get; set; }
        public string fechaExpiracionPoliza { get; set; }
        public bool asignacionActividadAltoRiesgo { get; set; }
        public string codigoMoneda { get; set; }
        public string codigoFrecuenciaPago { get; set; }
        public string codigoFrecuenciaRenovacion { get; set; }
        public string codigoFormaPago { get; set; }
        public bool asignacionFacturacionAnticipada { get; set; }
        public bool asignacionRegulaMesVencido { get; set; }
        public int? tipoFacturacionRMV { get; set; } //SCTR-1347
        public string codigoTipoTransaccion { get; set; }
        public string fechaTransaccion { get; set; }
        public string codigoUsuarioRegistro { get; set; }
        public decimal primaMinimaAutorizada { get; set; }
        public decimal primaComercial { get; set; }
        public decimal igv { get; set; }
        public decimal? derechoEmision { get; set; }
        public decimal primaTotal { get; set; }
        public string cipPagoEfectivo { get; set; }
        public string sede { get; set; }
        public List<riesgos> riesgos { get; set; }
        public List<asegurados> asegurados { get; set; }
        public List<Document_EPS> documentos { get; set; } // Relazamiento
    }

    public class dataTransaccionAnulacionMovimiento
    {
        public string codigoCotizacion { get; set; }
        public string codigoContrato { get; set; }
        public string codigoProceso { get; set; }
        public string codigoTipoTransaccion { get; set; }
        public string codigoUsuarioRegistro { get; set; }
    }

    public class riesgos
    {
        public string codigoProducto { get; set; }
        public string codigoPlan { get; set; }
        public string codigoCategoria { get; set; }
        public int cantidadTrabajador { get; set; }
        public decimal planillaTotal { get; set; }
        public decimal tasaAutorizada { get; set; }
    }

    public class asegurados
    {
        public string nombres { get; set; }
        public string apellidoPaterno { get; set; }
        public string apellidoMaterno { get; set; }
        public string codigoPlan { get; set; }
        public string codigoTipoDocumento { get; set; }
        public string numeroDocumento { get; set; }
        public string fechaNacimiento { get; set; }
        public decimal remuneracion { get; set; }
        public string codigoUnicoCliente { get; set; }
        public decimal primaCobrada { get; set; }
        public decimal igvCobrado { get; set; }
        public decimal derechoEmisionCobrado { get; set; }
        public decimal primaBrutaCobrada { get; set; }
        public string codigoGenero { get; set; }
        public string correoElectronico { get; set; }
        public string codigoTipoTelefono { get; set; }
        public string telefono { get; set; }
        public string codigoNeteo { get; set; }
    }

    public class poliza_eps_sctr
    {
        public int P_NCODE { get; set; }
        public Int64 P_NPOLICY { get; set; }
        public string P_MESSAGE { get; set; }
    }

    public class Ins_Documentacion_EPS
    {
        public string P_NID_PROC_EPS { get; set; }
        public int P_NBRANCH { get; set; }
        public int P_NPRODUCT { get; set; }
        public int P_NID_COTIZACION { get; set; }
        public long P_NPOLICY { get; set; }
        public int P_ID_DOCUMENTO { get; set; }
        public string P_DES_DOCUMENTO { get; set; }
        public int P_NSTATE { get; set; }
        public string P_MENSAJE_IMP { get; set; }
        public int P_NTYPE_TRANSAC { get; set; }
    }

    public class dataTecnicaTrans
    {
        public string NPOLICY { get; set; }
        public int NCURRENCY { get; set; }
        public double NPREMIUM_MIN_AU { get; set; }
        public double NSUM_PREMIUMN { get; set; }
        public double NSUM_IGV { get; set; }
        public double NDE { get; set; }
        public double NSUM_PREMIUM { get; set; }
        public string NID_PROC { get; set; }
        public int SDELIMITER { get; set; }
        public int FACTURA_ANT { get; set; }
        public int REGULA { get; set; }
    }

    public class ResponseSiniestros
    {
        public int P_FLAG { get; set; }
        public int P_COD_ERR { get; set; }
        public string P_MESSAGE { get; set; }
    }

    public class RequestSiniestros
    {
        public int P_NID_COTIZACION { get; set; }
        public string P_FECHA { get; set; }
    }

    public class ResponseEPSVM //SCTR-1294
    {
        public string P_NCODE { get; set; }
        public string P_SMESSAGE { get; set; }
        public int? P_FLAG { get; set; }
        public string P_NPOLICY { get; set; }
        public List<DocumentoEPSLight> documentos { get; set; } = new List<DocumentoEPSLight>();
    }

    public class DocumentoEPSLight //SCTR-1294
    {
        public int idContratoDocumento { get; set; }
        public string nombre { get; set; }
    }
}
