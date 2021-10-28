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

        public string IdMetadataArchivadorFacturasFechaRecpecion { get; set; }

        public string IdMetadataArchivadorFacturasTotalFactura { get; set; }

        public string IdMetadataArchivadorFacturasLibreNumero { get; set; }

        public string IdMetadataArchivadorFacturasLibreTexto { get; set; }

        public string IdMetadataArchivadorFacturasLibreFecha { get; set; }

        public string IdMetadataArchivadorFacturasLibreLista { get; set; }

        public string IdMetadataArchivadorFacturasTipoFactura { get; set; }

        public string IdMetadataArchivadorFacturasConcepto { get; set; }
        

        public string IdMetadataArchivadorFacturasFechaRegistroContable { get; set; }

        public string IdMetadataArchivadorFacturasNumeroAsientoContable { get; set; }

        public string IdMetadataArchivadorFacturasCodigoObra { get; set; }

        public string IdMetadataArchivadorFacturasNumeroPedido { get; set; }

                     
        //Metadatos del Registro Datos Facturas

        public string IdMetadataRegistroDatosFacturasCodigoFactura { get; set; }

        public string IdMetadataRegistroDatosFacturasSociedad { get; set; }

        public string IdMetadataRegistroDatosFacturasBaseSujeta { get; set; }

        public string IdMetadataRegistroDatosFacturasIVATipo { get; set; }

        public string IdMetadataRegistroDatosFacturasIVACuota { get; set; }

        public string IdMetadataRegistroDatosFacturasRecargoEquivalenciaTipo { get; set; }

        public string IdMetadataRegistroDatosFacturasRecargoEquivalenciaCuota { get; set; }

               

        public string IdWorkflow { get; set; }

        public string IdSalidaWorkFlowTareaPendienteEnvioAERP { get; set; }

        public string IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoAceptada { get; set; }

        public string IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoRechazada { get; set; }

        public string IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoPagada { get; set; }

        public string RutaLogs { get; set; }

        public string RutaDescarga { get; set; }
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

        public string TotalFactura { get; set; }

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

        public string Concepto { get; set; }

        public byte[] FacturaFile { get; set; }
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