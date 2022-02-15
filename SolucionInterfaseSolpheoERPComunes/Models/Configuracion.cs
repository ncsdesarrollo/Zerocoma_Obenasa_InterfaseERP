using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SolucionFacturasComunes.Models
{
    public class Configuracion
    {
        public string TiempoEsperaEntreEjecucionEnMinutos { get; set; }
        public string SolpheoTenant { get; set; }
               
        public string SolpheoUrl { get; set; }
        
        public string SolpheoUsuario { get; set; }
        public string SolpheoPassword { get; set; }

        public string XMLRutaEntradaERP { get; set; }

        public string XMLRutaSalidaERP { get; set; }

        public string CSVProveedoresRutaERP { get; set; }

        public string CSVCodigosObraRutaERP { get; set; }

        public string IdFileContainerArchivadorFacturas { get; set; }

        public string IdFileContainerRegistroDatosFacturas { get; set; }

        //Metadatos del Archivador Facturas

        public string IdMetadataArchivadorFacturasCodigoFactura { get; set; }

        public string IdMetadataArchivadorFacturasSociedad { get; set; }

        public string IdMetadataArchivadorFacturasEstado { get; set; }

        public string IdMetadataArchivadorFacturasEstadoProveedor { get; set; }

        public string IdMetadataArchivadorFacturasCIFProveedor { get; set; }

        public string IdMetadataArchivadorFacturasNumeroFactura { get; set; }

        public string IdMetadataArchivadorFacturasFechaEmision { get; set; }

        public string IdMetadataArchivadorFacturasFechaRecepcion { get; set; }

        public string IdMetadataArchivadorFacturasTotalFactura { get; set; }

        public string IdMetadataArchivadorFacturasLibreNumero { get; set; }

        public string IdMetadataArchivadorFacturasLibreTexto { get; set; }

        public string IdMetadataArchivadorFacturasLibreFecha { get; set; }

        public string IdMetadataArchivadorFacturasLibreLista { get; set; }

        public string IdMetadataArchivadorFacturasTipoFactura { get; set; }

        public string IdMetadataArchivadorFacturasComentarios { get; set; }

        public string IdMetadataArchivadorFacturasRazonSocialProveedor { get; set; }


        public string IdMetadataArchivadorFacturasFechaRegistroContable { get; set; }

        public string IdMetadataArchivadorFacturasNumeroAsientoContable { get; set; }

        public string IdMetadataArchivadorFacturasCodigoObra { get; set; }

        public string IdMetadataArchivadorFacturasNumeroPedido { get; set; }

                     
        //Metadatos del Archivador para los impuestos 

        public string IdMetadataArchivadorFacturasImpuesto1BaseSujeta { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto1IVATipo { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto1IVACuota { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto1RecargoEquivalenciaTipo { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto1RecargoEquivalenciaCuota { get; set; }


        public string IdMetadataArchivadorFacturasImpuesto2BaseSujeta { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto2IVATipo { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto2IVACuota { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto2RecargoEquivalenciaTipo { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto2RecargoEquivalenciaCuota { get; set; }


        public string IdMetadataArchivadorFacturasImpuesto3BaseSujeta { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto3IVATipo { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto3IVACuota { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto3RecargoEquivalenciaTipo { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto3RecargoEquivalenciaCuota { get; set; }



        public string IdMetadataArchivadorFacturasImpuesto4BaseSujeta { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto4IVATipo { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto4IVACuota { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto4RecargoEquivalenciaTipo { get; set; }

        public string IdMetadataArchivadorFacturasImpuesto4RecargoEquivalenciaCuota { get; set; }



        public string IdWorkflow { get; set; }

        public string IdSalidaWorkFlowTareaPendienteEnvioAERP { get; set; }

        public string IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoAceptada { get; set; }

        public string IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoRechazada { get; set; }

        public string IdSalidaWorkFlowTareaPendienteAprobacionPagoERP_ResultadoPagada { get; set; }

        public string TaskKeyTareaPendienteContabilizacionERP { get; set; }

        public string TaskKeyTareaPendienteAprobacionPagoERP { get; set; }

        public string RutaLogs { get; set; }

        public string URLVisorDocumentos { get; set; }

        public string URLAPIPortalProveedores { get; set; }

        public string ApiKeyAPIPortalProveedores { get; set; }

        public string EstadoFacturaPendienteContabilizada { get; set; }

        public string EstadoFacturaContabilizadaOK { get; set; }

        public string EstadoFacturaPendienteEnvioERP { get; set; }


    }

    public class ResultadoSolpheo
    {
        public string Mensaje { get; set; }
        public bool Resultado { get; set; }
    }
        public class XMLDatosCabecera
    {
        public string SolpheoIdMetadato { get; set; }
        public string SolpheoNameVariable { get; set; }
        public string SolpheoTipoMetadato { get; set; }
        public string XMLTagName { get; set; }
        public string XMLTagXPath { get; set; }
    }
    public class XMLDatosFacturas
    {
        public string XMLNodoPadreImpuestosXPath { get; set; }
        public List<XMLDatosCabecera> MapeoDatosImpuesto { get; set; }
    }   
    public class NeoDocMapeoRobotsTokens
    {
        public string SociedadSolpheo { get; set; }
        public string UUIDRobotAsociado { get; set; }
        public string TokenLoginRobotAsociado { get; set; }
    }
    public class JsonResponse
    {
        public string Estado { get; set; }
        public ListadoFacturas ListadoFacturas { get; set; }
        public string DescripcionError { get; set; }
    }
    public class ListadoFacturas
    {
        public List<Factura> Facturas { get; set; }
    }

    public class Factura
    {
        public string Identificador { get; set; }
        public string Sociedad { get; set; }

        public string Codigofactura { get; set; }

        public string CIFProveedor { get; set; }

        public string NumeroFactura { get; set; }

        public DateTime FechaEmision { get; set; }

        public DateTime FechaRecepcion { get; set; }

        public Decimal TotalFactura { get; set; }

        public string LibreNumero { get; set; }

        public string CodigoObra { get; set; }

        public string NumeroPedido { get; set; }

        public string TipoFactura { get; set; }
        

        public DateTime LibreFecha { get; set; }

        public string LibreLista { get; set; }

        public string RutaDescarga { get; set; }

        public string BaseSujeta { get; set; }

        public string TipoIVA { get; set; }

        public string CuotaIVA { get; set; }

        public string RecEq { get; set; }

        public string CuotaReq { get; set; }

        public string Comentarios { get; set; }

        public string RazonSocialProveedor { get; set; }


        public Decimal Impuesto1Base { get; set; }

        public string Impuesto1Tipo { get; set; }

        public Decimal Impuesto1Cuota { get; set; }

        public string Impuesto1RecEqTipo { get; set; }

        public Decimal Impuesto1RecEqCuota { get; set; }

        public Decimal Impuesto2Base { get; set; }

        public string Impuesto2Tipo { get; set; }

        public Decimal Impuesto2Cuota { get; set; }

        public string Impuesto2RecEqTipo { get; set; }

        public Decimal Impuesto2RecEqCuota { get; set; }

        public Decimal Impuesto3Base { get; set; }

        public string Impuesto3Tipo { get; set; }

        public Decimal Impuesto3Cuota { get; set; }

        public string Impuesto3RecEqTipo { get; set; }

        public Decimal Impuesto3RecEqCuota { get; set; }

        public Decimal Impuesto4Base { get; set; }

        public string Impuesto4Tipo { get; set; }

        public Decimal Impuesto4Cuota { get; set; }

        public string Impuesto4RecEqTipo { get; set; }

        public Decimal Impuesto4RecEqCuota { get; set; }

        public byte[] FacturaFile { get; set; }
    }
    
    public class FacturaObjetoXML
    {        
        public string EstadoXML { get; set; }
        public string LibreFechaDia { get; set; }
        public string LibreFechaMes { get; set; }
        public string LibreFechaAno { get; set; }
        public string LibreLista { get; set; }
    }
    
    public class SolpheoMapeoMetadatos
    {
        public int IdMetadato { get; set; }
        public string NameVariable { get; set; }
        public string Tipo { get; set; }
        public string Valor { get; set; }
        public string TagXML { get; set; }

    }

    public class RequestSendXML
    {
        public string XMLResultadoOCR { get; set; }
    }

    public class ResponseNeoDocAPI
    {
        public string ocr_status { get; set; }
    }
}