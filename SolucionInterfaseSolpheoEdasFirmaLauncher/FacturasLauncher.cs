using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using SolucionFacturasComunes.Models;
using SolucionFacturasComunes;
using Kyocera.Solpheo.ApiClient.Models;
using System.Xml;
using System.Xml.Schema;


namespace SolucionFacturasLauncher
{
    public class FacturasLauncher
    {
        public FacturasLauncherConfig config;
        public Configuracion jsonConfig;
        protected Timer clock;
        protected string tenant;
        private ILogger log;
        public int _cont = 0;
        public FacturasLauncher()
        {
        }
        public FacturasLauncher(FacturasLauncherConfig configuration)
        {
            config = configuration;
        }

        public void Start(String args)
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                  .ReadFrom.AppSettings()
                  .CreateLogger();

                log = Log.ForContext<FacturasLauncher>();

                if (String.IsNullOrEmpty(args))
                {
                    log.Error("GENÉRICO - Debe pasar un parámetro al servicio indicando el tenant");
                }
                else
                {

                    log.Information("GENÉRICO - Se va a procesar el archivo correspondiente al tenant {0}", args);
                    config = new FacturasLauncherConfig(args);

                    //int intervalo = int.Parse(config.Configuracion.TiempoEsperaEntreEjecucionEnMinutos);

                    Task tenantChecker = PeriodicAsync(async () =>
                    {
                        try
                        {
                            await CheckTenantAsync(config.Configuracion, log);
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                    }, TimeSpan.FromMinutes(int.Parse(config.Configuracion.TiempoEsperaEntreEjecucionEnMinutos)));
                }
            }

            catch (Exception e)
            {
                log.Error("GENÉRICO - Error durante la configuración del servicio. Error: {0}", e.Message);
            }
        }

        public void Stop()
        {
        }

        public static async Task PeriodicAsync(Func<Task> taskFactory, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var delayTask = Task.Delay(interval, cancellationToken);
                await taskFactory();
                await delayTask;
            }
        }

        protected async Task CheckTenantAsync(Configuracion JsonConfig, ILogger log)
        {
            log = Log.ForContext<FacturasLauncher>();

            try
            {
                // Leemos fichero de configuración y llamamos al api de Solpheo para obtener las facturas pendientes de enviar
                var response = await GetFacturasSolpheo(JsonConfig, tenant);

                if (response.Estado == "OK")
                {

                }

            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }


        public async Task<JsonResponse> GetFacturasSolpheo(Configuracion JsonConfig, string SolpheoIdTenant)
        {
            log.Information("GetFacturasSolpheo - Inicio método para el tenant - ", SolpheoIdTenant);

            var response = new JsonResponse();
            response.Estado = "OK";
            string Error = String.Empty;
            int idMetadatoArchivadorCodigoFactura = 0;
            int idMetadatoArchivadorSociedad = 0;
            int idMetadatoArchivadorCIFProveedor = 0;
            int idMetadatoArchivadorNumeroFactura = 0;
            int idMetadatoArchivadorFechaEmision = 0;
            int idMetadatoArchivadorFechaRecepcion = 0;
            int idMetadatoArchivadorTotalFactura = 0;
            int idMetadatoArchivadorLibreNumero = 0;
            int idMetadatoArchivadorCodigoObra = 0;
            int idMetadatoArchivadorNumeroPedido = 0;
            int idMetadatoArchivadorLibreFecha = 0;
            int idMetadatoArchivadorLibreLista = 0;
            int idMetadatoArchivadorTipoFactura = 0;
            int idMetadatoRegistroBase = 0;
            int idMetadatoRegistroTipo = 0;
            int idMetadatoRegistroCuotaIVA = 0;
            int idMetadatoRegistroRecEq = 0;
            int idMetadatoRegistroCuotaReq = 0;

            try
            {
                // Nos logamos en Solpheo con los datos obtenidos del json de configuracion
                var clienteSolpheo = new ClienteSolpheo(JsonConfig.SolpheoUrl);
                var loginSolpheo = await clienteSolpheo.LoginAsync(JsonConfig.SolpheoUsuario, JsonConfig.SolpheoPassword, JsonConfig.SolpheoTenant, "", "");

                // Nos traemos las facturas del archivador Facturas con estado "Pendiente enviar a ERP"
                string jsonFiltrado = "[{'typeOrAndSelected':'and','term':{'leftOperator':{'name':'Estado','description':'Estado','id': " + int.Parse(JsonConfig.IdMetadataArchivadorFacturasEstado) + ",'idType':1,'isProperty':false,'isContent':false},'rightOperator':'Pendiente enviar a ERP','type':0}}]";

                var documentosPendientesEnvio = await clienteSolpheo.FileItemsAdvancednested(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), jsonFiltrado);

                List<FileContainerListViewModel> FileItems = documentosPendientesEnvio.Items.ToList();

                log.Information("GetFacturasSolpheo - Se han encontrado {0} facturas pendientes de enviar a ERP", documentosPendientesEnvio.Items.Count());

                var Facturas = new List<Factura>();

                for (int i = 0; i < FileItems.Count; i++)
                {
                    var factura = new Factura();

                    factura.Identificador = FileItems[i].Id.ToString();

                    var respuestaMetadatos = await clienteSolpheo.MetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), FileItems[i].Id);

                    idMetadatoArchivadorCodigoFactura = int.Parse(JsonConfig.IdMetadataArchivadorFacturasCodigoFactura);
                    factura.Codigofactura = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorCodigoFactura).FirstOrDefault().StringValue;
                    factura.Codigofactura = "16592";

                    //Nos traemos los datos del registros DatosFactura de cada una de las facturas obtenidas anteriormente
                    string jsonFiltradoRegistro = "[{'typeOrAndSelected':'and','term':{'leftOperator':{'name':'Código_Factura','description':'Código_Factura','id': " + int.Parse(JsonConfig.IdMetadataRegistroDatosFacturasCodigoFactura) + ",'idType':1,'isProperty':false,'isContent':false},'rightOperator':" + factura.Codigofactura + ",'type':0}}]";

                    var documentosPendientesRegistro = await clienteSolpheo.FileItemsAdvancednested(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerRegistroDatosFacturas), jsonFiltradoRegistro);

                    List<FileContainerListViewModel> FileItemsRegistro = documentosPendientesRegistro.Items.ToList();

                    // Recuperamos los metadatos del archivador de facturas para poder insertarlos en el XML
                    idMetadatoArchivadorSociedad = int.Parse(JsonConfig.IdMetadataArchivadorFacturasSociedad);
                    factura.Sociedad = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorSociedad).FirstOrDefault().StringValue;

                    idMetadatoArchivadorCIFProveedor = int.Parse(JsonConfig.IdMetadataArchivadorFacturasCIFProveedor);
                    factura.CIFProveedor = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorCIFProveedor).FirstOrDefault().StringValue;

                    idMetadatoArchivadorNumeroFactura = int.Parse(JsonConfig.IdMetadataArchivadorFacturasNumeroFactura);
                    factura.NumeroFactura = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorNumeroFactura).FirstOrDefault().StringValue;

                    idMetadatoArchivadorFechaEmision = int.Parse(JsonConfig.IdMetadataArchivadorFacturasFechaEmision);
                    factura.FechaEmision = Convert.ToDateTime(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorFechaEmision).FirstOrDefault().StringValue);

                    idMetadatoArchivadorFechaRecepcion = int.Parse(JsonConfig.IdMetadataArchivadorFacturasFechaRecpecion);
                    factura.FechaRecepcion = Convert.ToDateTime(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorFechaRecepcion).FirstOrDefault().StringValue);

                    idMetadatoArchivadorTotalFactura = int.Parse(JsonConfig.IdMetadataArchivadorFacturasTotalFactura);
                    factura.TotalFactura = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorTotalFactura).FirstOrDefault().StringValue;

                    idMetadatoArchivadorLibreNumero = int.Parse(JsonConfig.IdMetadataArchivadorFacturasLibreNumero);
                    factura.LibreNumero = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorLibreNumero).FirstOrDefault().StringValue;

                    idMetadatoArchivadorCodigoObra = int.Parse(JsonConfig.IdMetadataArchivadorFacturasCodigoObra);
                    factura.CodigoObra = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorCodigoObra).FirstOrDefault().StringValue;

                    idMetadatoArchivadorNumeroPedido = int.Parse(JsonConfig.IdMetadataArchivadorFacturasNumeroPedido);
                    factura.NumeroPedido = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorNumeroPedido).FirstOrDefault().StringValue;

                    idMetadatoArchivadorLibreFecha = int.Parse(JsonConfig.IdMetadataArchivadorFacturasLibreFecha);
                    factura.LibreFecha = Convert.ToDateTime(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorLibreFecha).FirstOrDefault().StringValue);

                    idMetadatoArchivadorLibreLista = int.Parse(JsonConfig.IdMetadataArchivadorFacturasLibreLista);
                    factura.LibreLista = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorLibreLista).FirstOrDefault().StringValue;

                    idMetadatoArchivadorTipoFactura = int.Parse(JsonConfig.IdMetadataArchivadorFacturasTipoFactura);
                    factura.TipoFactura = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorTipoFactura).FirstOrDefault().StringValue;

                    factura.RutaDescarga = JsonConfig.RutaDescarga;

                    string RutaAccesoXMLEntradaERP = JsonConfig.XMLRutaEntradaERP + factura.Sociedad + @"\IN\";

                    if (!Directory.Exists(RutaAccesoXMLEntradaERP))
                    {
                        log.Error("GetFacturasSolpheo - Ruta Acceso XML Entrada ERP " + RutaAccesoXMLEntradaERP + " no existe para el idfileitem " + factura.Identificador);
                    }
                    else
                    {
                        XmlDocument doc = new XmlDocument();
                        XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                        XmlElement root = doc.DocumentElement;
                        doc.InsertBefore(xmlDeclaration, root);
                        XmlElement facturas = doc.CreateElement(string.Empty, "Facturas", string.Empty);
                        facturas.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                        facturas.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                        doc.AppendChild(facturas);
                        XmlElement fact = doc.CreateElement(string.Empty, "Factura", string.Empty);
                        facturas.AppendChild(fact);
                        XmlElement TipoAccion = doc.CreateElement(string.Empty, "Tipo", string.Empty);
                        XmlText tipoacciontexto = doc.CreateTextNode("RECIBIDA");
                        TipoAccion.AppendChild(tipoacciontexto);
                        fact.AppendChild(TipoAccion);
                        XmlElement numfact = doc.CreateElement(string.Empty, "Numero", string.Empty);
                        XmlText tipofact = doc.CreateTextNode(factura.NumeroFactura);
                        numfact.AppendChild(tipofact);
                        fact.AppendChild(numfact);
                        XmlElement FechaEmision = doc.CreateElement(string.Empty, "FechaEmision", string.Empty);
                        fact.AppendChild(FechaEmision);
                        XmlElement FechaEmisionDia = doc.CreateElement(string.Empty, "Dia", string.Empty);
                        XmlText DiaFE = doc.CreateTextNode(factura.FechaEmision.Day.ToString());
                        FechaEmisionDia.AppendChild(DiaFE);
                        FechaEmision.AppendChild(FechaEmisionDia);
                        XmlElement FechaEmisionMes = doc.CreateElement(string.Empty, "Mes", string.Empty);
                        XmlText MesFE = doc.CreateTextNode(factura.FechaEmision.Month.ToString());
                        FechaEmisionMes.AppendChild(MesFE);
                        FechaEmision.AppendChild(FechaEmisionMes);
                        XmlElement FechaEmisionAno = doc.CreateElement(string.Empty, "Ano", string.Empty);
                        XmlText AnoFE = doc.CreateTextNode(factura.FechaEmision.Year.ToString());
                        FechaEmisionAno.AppendChild(AnoFE);
                        FechaEmision.AppendChild(FechaEmisionAno);
                        XmlElement FechaRecepcion = doc.CreateElement(string.Empty, "FechaRecpecion", string.Empty);
                        fact.AppendChild(FechaRecepcion);
                        XmlElement FechaRecepcionDia = doc.CreateElement(string.Empty, "Dia", string.Empty);
                        XmlText DiaFR = doc.CreateTextNode(factura.FechaRecepcion.Day.ToString());
                        FechaRecepcionDia.AppendChild(DiaFR);
                        FechaRecepcion.AppendChild(FechaRecepcionDia);
                        XmlElement FechaRecepcionMes = doc.CreateElement(string.Empty, "Mes", string.Empty);
                        XmlText MesFR = doc.CreateTextNode(factura.FechaRecepcion.Month.ToString());
                        FechaRecepcionMes.AppendChild(MesFR);
                        FechaRecepcion.AppendChild(FechaRecepcionMes);
                        XmlElement FechaRecepcionAno = doc.CreateElement(string.Empty, "Ano", string.Empty);
                        XmlText AnoFR = doc.CreateTextNode(factura.FechaRecepcion.Year.ToString());
                        FechaRecepcionAno.AppendChild(AnoFR);
                        FechaRecepcion.AppendChild(FechaRecepcionAno);
                        XmlElement totfact = doc.CreateElement(string.Empty, "Total", string.Empty);
                        XmlText totfactTexto = doc.CreateTextNode(factura.TotalFactura);
                        totfact.AppendChild(totfactTexto);
                        fact.AppendChild(totfact);
                        XmlElement Entidad = doc.CreateElement(string.Empty, "Entidad", string.Empty);
                        fact.AppendChild(Entidad);
                        XmlElement CifEntidad = doc.CreateElement(string.Empty, "Cif", string.Empty);
                        XmlText Cif = doc.CreateTextNode(factura.CIFProveedor);
                        CifEntidad.AppendChild(Cif);
                        Entidad.AppendChild(CifEntidad);
                        XmlElement Impuestos = doc.CreateElement(string.Empty, "Impuestos", string.Empty);
                        fact.AppendChild(Impuestos);
                        foreach (var item in FileItemsRegistro)
                        {
                            var respuestaMetadatosRegistros = await clienteSolpheo.MetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerRegistroDatosFacturas), item.Id);

                            idMetadatoRegistroBase = int.Parse(JsonConfig.IdMetadataRegistroDatosFacturasBaseSujeta);
                            factura.BaseSujeta = respuestaMetadatosRegistros.Items.Where(m => m.IdMetadata == idMetadatoRegistroBase).FirstOrDefault().DecimalValue.ToString();
                            idMetadatoRegistroTipo = int.Parse(JsonConfig.IdMetadataRegistroDatosFacturasIVATipo);
                            factura.TipoIVA = respuestaMetadatosRegistros.Items.Where(m => m.IdMetadata == idMetadatoRegistroTipo).FirstOrDefault().DecimalValue.ToString();
                            idMetadatoRegistroCuotaIVA = int.Parse(JsonConfig.IdMetadataRegistroDatosFacturasIVACuota);
                            factura.CuotaIVA = respuestaMetadatosRegistros.Items.Where(m => m.IdMetadata == idMetadatoRegistroCuotaIVA).FirstOrDefault().DecimalValue.ToString();
                            idMetadatoRegistroRecEq = int.Parse(JsonConfig.IdMetadataRegistroDatosFacturasRecargoEquivalenciaTipo);
                            factura.RecEq = respuestaMetadatosRegistros.Items.Where(m => m.IdMetadata == idMetadatoRegistroRecEq).FirstOrDefault().DecimalValue.ToString();
                            idMetadatoRegistroCuotaReq = int.Parse(JsonConfig.IdMetadataRegistroDatosFacturasRecargoEquivalenciaCuota);
                            factura.CuotaReq = respuestaMetadatosRegistros.Items.Where(m => m.IdMetadata == idMetadatoRegistroCuotaReq).FirstOrDefault().DecimalValue.ToString();
                            XmlElement Impuesto = doc.CreateElement(string.Empty, "Impuesto", string.Empty);
                            Impuestos.AppendChild(Impuesto);
                            XmlElement Base = doc.CreateElement(string.Empty, "Base", string.Empty);
                            XmlText baseTexto = doc.CreateTextNode(factura.BaseSujeta);
                            Base.AppendChild(baseTexto);
                            Impuesto.AppendChild(Base);
                            XmlElement Tipo = doc.CreateElement(string.Empty, "Tipo", string.Empty);
                            XmlText tipoTexto = doc.CreateTextNode(factura.TipoIVA);
                            Tipo.AppendChild(tipoTexto);
                            Impuesto.AppendChild(Tipo);
                            XmlElement CuotaIVA = doc.CreateElement(string.Empty, "CuotaIVA", string.Empty);
                            XmlText cuotaIVATexto = doc.CreateTextNode(factura.CuotaIVA);
                            CuotaIVA.AppendChild(cuotaIVATexto);
                            Impuesto.AppendChild(CuotaIVA);
                            XmlElement RecEq = doc.CreateElement(string.Empty, "RecEq", string.Empty);
                            XmlText recEqTexto = doc.CreateTextNode(factura.RecEq);
                            RecEq.AppendChild(recEqTexto);
                            Impuesto.AppendChild(RecEq);
                            XmlElement CuotaReq = doc.CreateElement(string.Empty, "CuotaReq", string.Empty);
                            XmlText cuotaReqTexto = doc.CreateTextNode(factura.CuotaReq);
                            CuotaReq.AppendChild(cuotaReqTexto);
                            Impuesto.AppendChild(CuotaReq);
                        }

                        XmlElement fichero = doc.CreateElement(string.Empty, "Fichero", string.Empty);
                        XmlText fichTexto = doc.CreateTextNode(factura.Identificador);
                        fichero.AppendChild(fichTexto);
                        fact.AppendChild(fichero);
                        XmlElement LibreNumero = doc.CreateElement(string.Empty, "LibreNumero", string.Empty);
                        fact.AppendChild(LibreNumero);
                        XmlElement LibreTexto = doc.CreateElement(string.Empty, "LibreTexto", string.Empty);
                        XmlText libreTextoTexto = doc.CreateTextNode("CodObra: " + factura.CodigoObra + " - NumPedido: " + factura.NumeroPedido);
                        LibreTexto.AppendChild(libreTextoTexto);
                        fact.AppendChild(LibreTexto);
                        XmlElement LibreFecha = doc.CreateElement(string.Empty, "LibreFecha", string.Empty);
                        fact.AppendChild(LibreFecha);
                        XmlElement LibreFechaDia = doc.CreateElement(string.Empty, "Dia", string.Empty);
                        LibreFecha.AppendChild(LibreFechaDia);
                        XmlElement LibreFechaMes = doc.CreateElement(string.Empty, "Mes", string.Empty);
                        LibreFecha.AppendChild(LibreFechaMes);
                        XmlElement LibreFechaAno = doc.CreateElement(string.Empty, "Ano", string.Empty);
                        LibreFecha.AppendChild(LibreFechaAno);
                        XmlElement LibreLista = doc.CreateElement(string.Empty, "LibreLista", string.Empty);
                        XmlText libreListaTexto = doc.CreateTextNode(factura.TipoFactura);
                        LibreLista.AppendChild(libreListaTexto);
                        fact.AppendChild(LibreLista);
                        XmlElement comentario = doc.CreateElement(string.Empty, "Comentarios", string.Empty);
                        XmlText comentarioTexto = doc.CreateTextNode(factura.RutaDescarga + factura.Identificador);
                        comentario.AppendChild(comentarioTexto);
                        fact.AppendChild(comentario);
                        doc.Save(RutaAccesoXMLEntradaERP + factura.Identificador + ".xml");

                        //Actualizamos las variables del WF al estado "Pendiente Resultado EdasFirma"
                        var resultIdWF = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(factura.Identificador));
                        if (resultIdWF.Mensaje != "null")
                        {
                            var avance = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(factura.Identificador), JsonConfig.IdSalidaWorkFlowTareaPendienteEnvioAERP, int.Parse(resultIdWF.Mensaje));
                            if (!avance.Resultado)
                            {
                                log.Error("GetFacturasSolpheo - Error al avanzar Workflow tras enviar documento a ERP con IdFileItem " + factura.Identificador);
                            }
                        }
                    }
                    Facturas.Add(factura);
                }

                //Comprobamos las facturas que se encuentran en estado = "Pendiente Resultado Contabilización ERP"
                string jsonFiltradoResultado = "[{'typeOrAndSelected':'and','term':{'leftOperator':{'name':'Estado','description':'Estado','id': " + int.Parse(JsonConfig.IdMetadataArchivadorFacturasEstado) + ",'idType':1,'isProperty':false,'isContent':false},'rightOperator':'Pendiente Respuesta ERP','type':0}}]";
                var documentosPendientesResultado = await clienteSolpheo.FileItemsAdvancednested(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), jsonFiltradoResultado);

                List<FileContainerListViewModel> FileItemsResultado = documentosPendientesResultado.Items.ToList();

                var FacturasResultado = new List<Factura>();
                bool Encontrado = false;
                string IdentificadorRespuesta = "";

                for (int z = 0; z < FileItemsResultado.Count; z++)
                {
                    var facturaResultado = new Factura();

                    facturaResultado.Identificador = FileItemsResultado[z].Id.ToString();

                    Encontrado = true;
                    string Comentario = "";
                    string LibreFechaDia = "";
                    string LibreFechaMes = "";
                    string LibreFechaAno = "";
                    string LibreLista = "";
                    //por cada una de las facturas que tenemos en Pendiente Resultado Contabilización ERP, recuperamos su sociedad para poder crear la ruta de salida
                    var respuestaMetadatos = await clienteSolpheo.MetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), int.Parse(facturaResultado.Identificador));

                    idMetadatoArchivadorSociedad = int.Parse(JsonConfig.IdMetadataArchivadorFacturasSociedad);
                    facturaResultado.Sociedad = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorSociedad).FirstOrDefault().StringValue;

                    string RutaAccesoSalidaERP = JsonConfig.XMLRutaEntradaERP + facturaResultado.Sociedad + @"\OUT\";

                    if (!Directory.Exists(RutaAccesoSalidaERP))
                    {
                        log.Error("GetFacturasSolpheo - Ruta Acceso Salida ERP " + RutaAccesoSalidaERP + " no existe");
                    }
                    else
                    {
                        var filenameSalida = RutaAccesoSalidaERP + facturaResultado.Identificador + ".XML";
                        if (File.Exists(filenameSalida))
                        {
                            XmlDocument xDoc = new XmlDocument();
                            xDoc.Load(filenameSalida);

                            XmlNodeList elemList = xDoc.GetElementsByTagName("Fichero");
                            for (int a = 0; a < elemList.Count; a++)
                            {
                                if (elemList[a].InnerXml == facturaResultado.Identificador)
                                {
                                    XmlNodeList elemListComentario = xDoc.GetElementsByTagName("Comentarios");
                                    Comentario = elemListComentario[a].InnerText;
                                    XmlNodeList elemListLibreFechaDia = xDoc.GetElementsByTagName("LibreFecha");
                                    LibreFechaDia = elemListLibreFechaDia[a].ChildNodes[0].InnerText;
                                    LibreFechaMes = elemListLibreFechaDia[a].ChildNodes[1].InnerText;
                                    LibreFechaAno = elemListLibreFechaDia[a].ChildNodes[2].InnerText;
                                    XmlNodeList elemListLibreLista = xDoc.GetElementsByTagName("LibreLista");
                                    LibreLista = elemListLibreLista[a].InnerText;
                                }
                            }
                            if (Comentario.ToUpper() == "RECHAZADA")
                            {
                                //Actualizamos las variables del WF al estado "Rechazada"
                                var resultIdWFSalida = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(facturaResultado.Identificador));
                                var avancesalida = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(facturaResultado.Identificador), JsonConfig.IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoRechazada, int.Parse(resultIdWFSalida.Mensaje));
                                if (!avancesalida.Resultado)
                                {
                                    //si da error se informa en el log y se mueve el XML a una subcarpeta KO
                                    log.Error("Avanzar Workflow - Error al avanzar Workflow a estado Rechazada para el IdFileItem " + facturaResultado.Identificador);
                                    string path = filenameSalida;
                                    string directoriosalidacopiado = RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_KO\";
                                    string ficherosalidacopiado = directoriosalidacopiado + facturaResultado.Identificador + ".XML";
                                    if (!Directory.Exists(directoriosalidacopiado))
                                    {
                                        Directory.CreateDirectory(directoriosalidacopiado);
                                    }
                                    File.Move(path, ficherosalidacopiado);
                                }
                                else
                                {
                                    //si ha ido bien, se mueve el XML a una subcarpeta OK
                                    string path = filenameSalida;
                                    string directoriosalidacopiado = RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_OK\";
                                    string ficherosalidacopiado = directoriosalidacopiado + facturaResultado.Identificador + ".XML";
                                    if (!Directory.Exists(directoriosalidacopiado))
                                    {
                                        Directory.CreateDirectory(directoriosalidacopiado);
                                    }
                                    File.Move(path, ficherosalidacopiado);
                                }
                            }
                            else if (Comentario.ToUpper() == "ACEPTADA")
                            {
                                //grabamos la fecha contable y libre lista en sus metadatos
                                var resultIdWFCambioMetadatos = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(facturaResultado.Identificador));
                                string FechaContable = "";
                                int variableSalidaResultado = 0;
                                int variableSalidaLLResultado = 0;
                                if (LibreFechaDia != "")
                                {
                                    FechaContable = LibreFechaDia + "/" + LibreFechaMes + "/" + LibreFechaAno;
                                    var variableSalida = await clienteSolpheo.ActualizarMetadato(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), int.Parse(facturaResultado.Identificador), int.Parse(JsonConfig.IdMetadataArchivadorFacturasFechaRegistroContable), "Fecha Registro Contable", "DateTimeValue", FechaContable);
                                    variableSalidaResultado = variableSalida.Id;
                                }
                                if (LibreLista != "")
                                {
                                    var variableSalidaLL = await clienteSolpheo.ActualizarMetadato(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas),int.Parse(facturaResultado.Identificador), int.Parse(JsonConfig.IdMetadataArchivadorFacturasLibreLista), "Libre Lista", "StringValue", LibreLista);
                                    variableSalidaLLResultado = variableSalidaLL.Id;
                                }
                                //si da error, mostrar mensaje y mover XML
                                if (variableSalidaResultado >= 0 || variableSalidaLLResultado >= 0)
                                {
                                    log.Error("Avanzar Workflow - Error al actualizar la fecharegistrocontable o número de asiento tras recibir XML con resultado contabilización del ERP con idfileitem " + facturaResultado.Identificador);
                                    string path = filenameSalida;
                                    string directoriosalidacopiado = RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_KO\";
                                    string ficherosalidacopiado = directoriosalidacopiado + facturaResultado.Identificador + ".XML";
                                    if (!Directory.Exists(directoriosalidacopiado))
                                    {
                                        Directory.CreateDirectory(directoriosalidacopiado);
                                    }
                                    File.Move(path, ficherosalidacopiado);
                                }
                                //Avanzamos el WF al estado = aceptada
                                var resultIdWFSalida = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(facturaResultado.Identificador));
                                var avancesalida = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(facturaResultado.Identificador), JsonConfig.IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoAceptada, int.Parse(resultIdWFSalida.Mensaje));
                                if (!avancesalida.Resultado)
                                {
                                    //si da error se informa en el log y se mueve el XML a una subcarpeta KO
                                    log.Error("Avanzar Workflow - Error al avanzar Workflow tras recibir XML con resultado contabilización del ERP para el IdFileItem " + facturaResultado.Identificador);
                                    string path = filenameSalida;
                                    string directoriosalidacopiado = RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_KO\";
                                    string ficherosalidacopiado = directoriosalidacopiado + facturaResultado.Identificador + ".XML";
                                    if (!Directory.Exists(directoriosalidacopiado))
                                    {
                                        Directory.CreateDirectory(directoriosalidacopiado);
                                    }
                                    File.Move(path, ficherosalidacopiado);
                                }
                                else
                                {
                                    //si ha ido bien, se mueve el XML a una subcarpeta OK
                                    string path = filenameSalida;
                                    string directoriosalidacopiado = RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_OK\";
                                    string ficherosalidacopiado = directoriosalidacopiado + facturaResultado.Identificador + ".XML";
                                    if (!Directory.Exists(directoriosalidacopiado))
                                    {
                                        Directory.CreateDirectory(directoriosalidacopiado);
                                    }
                                    File.Move(path, ficherosalidacopiado);
                                }
                            }
                        }
                    }
                }

                //Comprobamos las facturas que se encuentran en estado = "Aceptada"
                string jsonFiltradoPendientePago = "[{'typeOrAndSelected':'and','term':{'leftOperator':{'name':'Estado','description':'Estado','id': " + int.Parse(JsonConfig.IdMetadataArchivadorFacturasEstado) + ",'idType':1,'isProperty':false,'isContent':false},'rightOperator':'Aceptada','type':0}}]";
                var documentosPendientesPago = await clienteSolpheo.FileItemsAdvancednested(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), jsonFiltradoPendientePago);

                List<FileContainerListViewModel> FileItemsPendientePago = documentosPendientesPago.Items.ToList();

                var FacturasPendientePago = new List<Factura>();

                for (int z = 0; z < FileItemsPendientePago.Count; z++)
                {
                    var facturaPendientePago = new Factura();

                    facturaPendientePago.Identificador = FileItemsPendientePago[z].Id.ToString();

                    string Comentario = "";
                    //por cada una de las facturas que tenemos en Aceptada, recuperamos su sociedad para poder crear la ruta de salida
                    var respuestaMetadatos = await clienteSolpheo.MetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), int.Parse(facturaPendientePago.Identificador));

                    facturaPendientePago.Sociedad = respuestaMetadatos.Items.Where(m => m.IdMetadata == int.Parse(JsonConfig.IdMetadataArchivadorFacturasSociedad)).FirstOrDefault().StringValue;

                    string RutaAccesoSalidaERP = JsonConfig.XMLRutaEntradaERP + facturaPendientePago.Sociedad + @"\OUT\";

                    if (!Directory.Exists(RutaAccesoSalidaERP))
                    {
                        log.Error("GetFacturasSolpheo - Ruta Acceso Salida ERP " + RutaAccesoSalidaERP + " no existe");
                    }
                    else
                    {
                        var filenameSalida = RutaAccesoSalidaERP + facturaPendientePago.Identificador + ".XML";
                        if (File.Exists(filenameSalida))
                        {
                            XmlDocument xDoc = new XmlDocument();
                            xDoc.Load(filenameSalida);

                            XmlNodeList elemList = xDoc.GetElementsByTagName("Fichero");
                            for (int a = 0; a < elemList.Count; a++)
                            {
                                if (elemList[a].InnerXml == facturaPendientePago.Identificador)
                                {
                                    XmlNodeList elemListComentario = xDoc.GetElementsByTagName("Comentarios");
                                    Comentario = elemListComentario[a].InnerText;
                                }
                            }
                            if (Comentario.ToUpper() == "PAGADA")
                            {
                                //Actualizamos las variables del WF al estado "Pagada"
                                var resultIdWFSalida = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(facturaPendientePago.Identificador));
                                var avancesalida = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(facturaPendientePago.Identificador), JsonConfig.IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoPagada, int.Parse(resultIdWFSalida.Mensaje));
                                if (!avancesalida.Resultado)
                                {
                                    //si da error se informa en el log y se mueve el XML a una subcarpeta KO
                                    log.Error("Avanzar Workflow - Error al avanzar Workflow a estado Pagada para el IdFileItem " + facturaPendientePago.Identificador);
                                    string path = filenameSalida;
                                    string directoriosalidacopiado = RutaAccesoSalidaERP + @"\PAGO_PROCESADO_KO\";
                                    string ficherosalidacopiado = directoriosalidacopiado + facturaPendientePago.Identificador + ".XML";
                                    if (!Directory.Exists(directoriosalidacopiado))
                                    {
                                        Directory.CreateDirectory(directoriosalidacopiado);
                                    }
                                    File.Move(path, ficherosalidacopiado);
                                }
                                else
                                {
                                    //si ha ido bien, se mueve el XML a una subcarpeta OK
                                    string path = filenameSalida;
                                    string directoriosalidacopiado = RutaAccesoSalidaERP + @"\PAGO_PROCESADO_OK\";
                                    string ficherosalidacopiado = directoriosalidacopiado + facturaPendientePago.Identificador + ".XML";
                                    if (!Directory.Exists(directoriosalidacopiado))
                                    {
                                        Directory.CreateDirectory(directoriosalidacopiado);
                                    }
                                    File.Move(path, ficherosalidacopiado);
                                }
                            }
                        }
                    }
                }

                //Carga de codigos de obra en el portal de proveedores
                response = await FicheroCSV_CodigoObra(JsonConfig);

                //Carga de proveedores en el portal de proveedores
                response = await FicheroCSV_Proveedores(JsonConfig);

                var ListadoFacturas = new ListadoFacturas();
                ListadoFacturas.Facturas = Facturas;
                response.ListadoFacturas = ListadoFacturas;

                log.Information("GetFacturasSolpheo - Fin método");
            }

            catch (Exception ex)
            {
                log.Error("GetFacturasSolpheo - Error", "", ex.Message);
            }

            if (!String.IsNullOrEmpty(Error))
            {
                response.DescripcionError = Error;
                response.Estado = "KO";
            }

            return response;
        }

        public async Task<JsonResponse> FicheroCSV_CodigoObra(Configuracion JsonConfig)
        {
            var response = new JsonResponse();
            //Leer fichero CSV de codigo de obra
            string rutaCSVCodigoObra = JsonConfig.CSVCodigosObraRutaERP + @"\CodigoObra.csv";
            var reader = new StreamReader(File.OpenRead(rutaCSVCodigoObra));
            List<string> listA = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(';');
                if (values[0] != "CodigoObra")
                {
                    listA.Add(values[0]);
                }
            }
            for (int i = 0; i < listA.Count; i++)
            {
                response = await GrabaCodigoObra(listA[i].ToString());
            }

            if (_cont == 0)
            {
                //Si ha ido bien, se moverá el fichero en una ruta de Procesados_OK
                string directoriosalidacopiadoCodigoObra = JsonConfig.CSVCodigosObraRutaERP + @"\PROCESADO_OK\";
                string ficherosalidacopiadoCodigoObra = directoriosalidacopiadoCodigoObra + @"\CodigoObra.csv";
                if (!Directory.Exists(directoriosalidacopiadoCodigoObra))
                {
                    Directory.CreateDirectory(directoriosalidacopiadoCodigoObra);
                }
                File.Move(rutaCSVCodigoObra, ficherosalidacopiadoCodigoObra);
            }
            else
            {
                //si ha ido mal alguna de las inserciones de codigos de obra
                string directoriosalidacopiadoCodigoObra = JsonConfig.CSVCodigosObraRutaERP + @"\PROCESADO_KO\";
                string ficherosalidacopiadoCodigoObra = directoriosalidacopiadoCodigoObra + @"\CodigoObra.csv";
                if (!Directory.Exists(directoriosalidacopiadoCodigoObra))
                {
                    Directory.CreateDirectory(directoriosalidacopiadoCodigoObra);
                }
                File.Move(rutaCSVCodigoObra, ficherosalidacopiadoCodigoObra);
            }


            return response;
        }

        public async Task<JsonResponse> GrabaCodigoObra(string id)
        {
            var response = new JsonResponse();
            var url = $"https://api-portalproveedores-obenasa.ncs-spain.com/";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("apikey", "432dd8d9d39d5b8c7592614d636a397c");

                    var content = new StringContent(JsonConvert.SerializeObject(id), System.Text.Encoding.UTF8, "application/json");

                    var responseClient = client.PostAsync(new Uri(string.Format(url, $"/ePortalProveedores/AltaCodigoObra/CodigoObra/")), content).Result;
                    if (responseClient.IsSuccessStatusCode)
                    {
                        string data = await responseClient.Content.ReadAsStringAsync();
                        var resultado = responseClient.Content.ReadAsStringAsync().Result;
                        return response;
                    }
                    else
                    {
                        var resultado = responseClient.Content.ReadAsStringAsync().Result;
                        _cont = _cont + 1;
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Grabar Codigo Obra - No se ha podido grabar el codigo de obra " + id + " en el portal de proveedores ");
                return response;
            }
        }

        public async Task<JsonResponse> FicheroCSV_Proveedores(Configuracion JsonConfig)
        {
            var response = new JsonResponse();
            //Leer fichero CSV de proveedores
            string rutaCSVProveedores = JsonConfig.CSVProveedoresRutaERP + @"\Proveedores.csv";
            var reader = new StreamReader(File.OpenRead(rutaCSVProveedores));
            List<string> listA = new List<string>();
            List<string> listB = new List<string>();
            List<string> listC = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(';');
                if (values[0] != "Nombre Proveedor")
                {
                    listA.Add(values[0]);
                    listB.Add(values[1]);
                    listC.Add(values[2]);
                }
            }
            for (int i = 0; i < listA.Count; i++)
            {
                response = await GrabaProveedor(listA[i].ToString(), listB[i].ToString(), listC[i].ToString());
            }

            if (_cont == 0)
            {
                //Si ha ido bien, se moverá el fichero en una ruta de Procesados_OK
                string directoriosalidacopiadoProveedor = JsonConfig.CSVCodigosObraRutaERP + @"\PROCESADO_OK\";
                string ficherosalidacopiadoProveedor = directoriosalidacopiadoProveedor + @"\CodigoObra.csv";
                if (!Directory.Exists(directoriosalidacopiadoProveedor))
                {
                    Directory.CreateDirectory(directoriosalidacopiadoProveedor);
                }
                File.Move(rutaCSVProveedores, ficherosalidacopiadoProveedor);
            }
            else
            {
                //si ha ido mal alguna de las inserciones de codigos de obra
                string directoriosalidacopiadoProveedor = JsonConfig.CSVCodigosObraRutaERP + @"\PROCESADO_KO\";
                string ficherosalidacopiadoProveedor = directoriosalidacopiadoProveedor + @"\CodigoObra.csv";
                if (!Directory.Exists(directoriosalidacopiadoProveedor))
                {
                    Directory.CreateDirectory(directoriosalidacopiadoProveedor);
                }
                File.Move(rutaCSVProveedores, ficherosalidacopiadoProveedor);
            }


            return response;
        }

        public async Task<JsonResponse> GrabaProveedor(string Nombre, string CIF, string Email)
        {
            var response = new JsonResponse();
            var url = $"https://api-portalproveedores-obenasa.ncs-spain.com/";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("apikey", "432dd8d9d39d5b8c7592614d636a397c");

                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("Nombre", Nombre),
                        new KeyValuePair<string, string>("NIF", CIF),
                        new KeyValuePair<string, string>("Email", Email),
                    });

                    var responseClient = client.PostAsync(new Uri(string.Format(url, $"/ePortalProveedores/AltaProveedores/")), formContent).Result;
                    if (responseClient.IsSuccessStatusCode)
                    {
                        string data = await responseClient.Content.ReadAsStringAsync();
                        var resultado = responseClient.Content.ReadAsStringAsync().Result;
                        return response;
                    }
                    else
                    {
                        var resultado = responseClient.Content.ReadAsStringAsync().Result;
                        _cont = _cont + 1;
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Grabar Proveedores - No se ha podido grabar el proveedor " + Nombre + " en el portal de proveedores ");
                return response;
            }
        }

    }
}
