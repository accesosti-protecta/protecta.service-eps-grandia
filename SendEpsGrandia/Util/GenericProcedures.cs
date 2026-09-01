using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Util
{
    public sealed class GenericProcedures
    {
        private GenericProcedures() { }

        //public static readonly string pkg_CargaMasivaPD = "PKG_CARGA_MASIVA_IMPRESION_PBQ";
        public static readonly string pkg_CargaMasivaPD = "PKG_CARGA_MASIVA_IMPRESION";
        public static readonly string pkg_CargaMasiva = "PKG_PV_CARGA_MASIVA";
        public static readonly string pkg_Condicionados = "PKG_PV_CONDICIONADOS";
        public static readonly string pkg_EstadoCiente = "PKG_PV_VAL_BLOCK_CLIENT";
        public static readonly string pkg_Laft = "LAFT.PKG_LAFT_DOCUMENTOS";
        public static readonly string pkg_MovementsEndosoVCF = "PKG_MOVEMENTS_ENDOSO";

        public static readonly string pkg_Eps = "PKG_PV_TRAT_EPS";

        public static readonly string sp_LeerPolizaEstado = "PD_REA_POL_ESTADO";
        public static readonly string sp_LeerEnfermedadesCobertura = "SP_REA_COVER_DISEASES";
        public static readonly string sp_LeerPolizasBelcorp = "INSUDB.SP_VI_REA_MASTER_DATA";
        public static readonly string sp_CargaBelcorp = "SP_CARGA_BELCORP";
        public static readonly string sp_UpdateCargaBelcorp = "UPD_STATE_CARGA";
        public static readonly string sp_LeerFormatosOtros = "REA_FORMATOS_OTHERS";
        public static readonly string sp_LeerFormatosLaft = "REA_FORMATOS_LAFT";
        public static readonly string sp_LeerFormatosMovementsEndosoVCF = "REA_FORMATOS_MOVEMENTS_ENDOSO";
        public static readonly string sp_InsLogPolizaCarga = "INS_PRINT_OTHERS_LOG";
        public static readonly string sp_InsertarLog = "INS_PRINT_OTHERS_LOG";
        public static readonly string sp_ActualizarEstadoComprobante = "SP_UPD_ESTADO_COMPROBANTES_EPS";
        public static readonly string sp_RelanzarEPS = "SP_GET_RELANZAMIENTO_EPS"; //INS_TBL_PD_LOG_AUTH
        public static readonly string sp_InsertarLogAuth = "INS_TBL_PD_LOG_AUTH";


        //Inicio JLE inclusion SCTR
        public static readonly string pkg_DocumentosSCTR = "PKG_PV_DOCUMENTOS";
        //Fin JLE inclusion SCTR
        public static readonly string pkg_EPS = "PKG_PV_TRAT_EPS";
        public static readonly string pkg_Poliza = "PKG_PV_TRAT_POLICY";//"PKG_PV_TRAT_POLICY"; // _RI

        public static readonly string sp_SendEPSJob = "SP_GET_PENDING_EPS_JOBS";
        public static readonly string sp_GetComprobantesJob = "SP_GET_COMPROBANTES_PENDIENTE_EPS";


    }
}
