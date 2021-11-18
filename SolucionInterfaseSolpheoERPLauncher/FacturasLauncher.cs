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
        int idMetadatoArchivadorEstado = 0;
        int idMetadatoRegistroBase = 0;
        int idMetadatoRegistroTipo = 0;
        int idMetadatoRegistroCuotaIVA = 0;
        int idMetadatoRegistroRecEq = 0;
        int idMetadatoRegistroCuotaReq = 0;
        int idMetadatoArchivadorComentarios = 0;
        int idMetadatoArchivadorRazonSocialProveedor = 0;

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

                    string outputTemplateDefault = ConfigurationManager.AppSettings["serilog:write-to:RollingFile.outputTemplate"];

                    Log.Logger = new LoggerConfiguration()
                            .WriteTo.Async(a =>
                            {
                                a.RollingFile(config.LogTenant, outputTemplate: !String.IsNullOrEmpty(outputTemplateDefault) ? outputTemplateDefault : "{Level} {Timestamp:yyyy-MM-dd HH:mm:ss} {Message}{NewLine}{Exception}");
                            })
                            .MinimumLevel.Verbose().CreateLogger();

                    log = Log.ForContext<FacturasLauncher>();

                    tenant = args;
                    jsonConfig = config.Configuracion;
                    log.Information($"Inicio - Tiempo de espera configurado en minutos " + jsonConfig.TiempoEsperaEntreEjecucionEnMinutos);
                    log.Information($"Inicio - Url del API Solpheo " + jsonConfig.SolpheoUrl);

                    int intervalo = int.Parse(jsonConfig.TiempoEsperaEntreEjecucionEnMinutos);

                    Task tenantChecker = PeriodicAsync(async () =>
                    {
                        try
                        {
                            await CheckTenantAsync(jsonConfig, log);
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                    }, TimeSpan.FromMinutes(intervalo));
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
                await GetFacturasSolpheo(JsonConfig, tenant);

            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }


        public async Task<bool> GetFacturasSolpheo(Configuracion JsonConfig, string SolpheoIdTenant)
        {
            log.Information("------------------ Inicio vuelta Servicio ---------------------");
            log.Information("GetFacturasSolpheo - Inicio método para el tenant - ", SolpheoIdTenant);

            var response = new JsonResponse();
            bool resultadoOK = true;
            string Error = String.Empty;


            try
            {
                // Nos logamos en Solpheo con los datos obtenidos del json de configuracion
                var clienteSolpheo = new ClienteSolpheo(JsonConfig.SolpheoUrl);
                var loginSolpheo = await clienteSolpheo.LoginAsync(JsonConfig.SolpheoUsuario, JsonConfig.SolpheoPassword, JsonConfig.SolpheoTenant, "multifuncional", "MfpSecret", "api");

                string RutaAccesoXMLEntradaERP = JsonConfig.XMLRutaEntradaERP;

                if (!Directory.Exists(RutaAccesoXMLEntradaERP))
                {
                    log.Error("GetFacturasSolpheo - Ruta Acceso XML Entrada ERP " + RutaAccesoXMLEntradaERP + " no existe. No se generarán los XML de las facturas pendientes de enviar a dicho directorio de entrada del ERP");
                }
                else
                {
                    // Nos traemos las facturas del archivador Facturas con estado "Pendiente enviar a ERP"
                    string jsonFiltrado = "[{'typeOrAndSelected':'and','term':{'leftOperator':{'name':'Estado','description':'Estado','id': " + int.Parse(JsonConfig.IdMetadataArchivadorFacturasEstado) + ",'idType':1,'isProperty':false,'isContent':false},'rightOperator':'" + JsonConfig.EstadoFacturaPendienteEnvioERP + "','type':0}}]";

                    var documentosPendientesEnvio = await clienteSolpheo.FileItemsAdvancednested(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), jsonFiltrado);

                    List<FileContainerListViewModel> FileItems = documentosPendientesEnvio.Items.ToList();

                    log.Information("GetFacturasSolpheo - Se han encontrado {0} facturas pendientes de enviar a ERP", documentosPendientesEnvio.Items.Count());

                    var Facturas = new List<Factura>();

                    for (int i = 0; i < FileItems.Count; i++)
                    {
                        var factura = new Factura();

                        factura.Identificador = FileItems[i].Id.ToString();

                        try
                        {
                            log.Information($"GetFacturasSolpheo - Generando XML de salida del fileItem {factura.Identificador}");

                            var respuestaMetadatos = await clienteSolpheo.MetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), FileItems[i].Id);

                            idMetadatoArchivadorCodigoFactura = int.Parse(JsonConfig.IdMetadataArchivadorFacturasCodigoFactura);
                            factura.Codigofactura = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorCodigoFactura).FirstOrDefault().StringValue;

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
                            if ((respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorFechaEmision).FirstOrDefault().DateTimeValue) != null)
                                factura.FechaEmision = Convert.ToDateTime(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorFechaEmision).FirstOrDefault().DateTimeValue.Value);
                            else
                                factura.FechaEmision = Convert.ToDateTime("01/01/2001");

                            idMetadatoArchivadorFechaRecepcion = int.Parse(JsonConfig.IdMetadataArchivadorFacturasFechaRecepcion);
                            if ((respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorFechaRecepcion).FirstOrDefault().DateTimeValue) != null)
                                factura.FechaRecepcion = Convert.ToDateTime(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorFechaRecepcion).FirstOrDefault().DateTimeValue.Value);
                            else
                                factura.FechaRecepcion = Convert.ToDateTime("01/01/2001");

                            idMetadatoArchivadorTotalFactura = int.Parse(JsonConfig.IdMetadataArchivadorFacturasTotalFactura);
                            factura.TotalFactura = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorTotalFactura).FirstOrDefault().DecimalValue);

                            idMetadatoArchivadorLibreNumero = int.Parse(JsonConfig.IdMetadataArchivadorFacturasLibreNumero);
                            factura.LibreNumero = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorLibreNumero).FirstOrDefault().StringValue;

                            idMetadatoArchivadorCodigoObra = int.Parse(JsonConfig.IdMetadataArchivadorFacturasCodigoObra);
                            factura.CodigoObra = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorCodigoObra).FirstOrDefault().StringValue;

                            idMetadatoArchivadorNumeroPedido = int.Parse(JsonConfig.IdMetadataArchivadorFacturasNumeroPedido);
                            factura.NumeroPedido = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorNumeroPedido).FirstOrDefault().StringValue;

                            idMetadatoArchivadorLibreFecha = int.Parse(JsonConfig.IdMetadataArchivadorFacturasLibreFecha);

                            if ((respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorLibreFecha).FirstOrDefault().DateTimeValue) != null)
                                factura.LibreFecha = Convert.ToDateTime(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorLibreFecha).FirstOrDefault().DateTimeValue.Value);
                            else
                                factura.LibreFecha = Convert.ToDateTime("01/01/2001");

                            idMetadatoArchivadorLibreLista = int.Parse(JsonConfig.IdMetadataArchivadorFacturasLibreLista);
                            factura.LibreLista = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorLibreLista).FirstOrDefault().StringValue;

                            idMetadatoArchivadorTipoFactura = int.Parse(JsonConfig.IdMetadataArchivadorFacturasTipoFactura);
                            factura.TipoFactura = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorTipoFactura).FirstOrDefault().StringValue;

                            idMetadatoArchivadorComentarios = int.Parse(JsonConfig.IdMetadataArchivadorFacturasComentarios);
                            factura.Comentarios = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorComentarios).FirstOrDefault().StringValue;

                            idMetadatoArchivadorRazonSocialProveedor = int.Parse(JsonConfig.IdMetadataArchivadorFacturasRazonSocialProveedor);
                            factura.RazonSocialProveedor = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorRazonSocialProveedor).FirstOrDefault().StringValue;

                            factura.RutaDescarga = JsonConfig.URLVisorDocumentos;

                            XmlDocument doc = await GenerarXMLFactura(clienteSolpheo, loginSolpheo, JsonConfig, factura, FileItemsRegistro);

                            doc.Save(RutaAccesoXMLEntradaERP + "\\" + factura.Identificador + ".xml");

                            //Se avanza el WF al estado "Pendiente Respuesta Contabilización ERP"
                            var resultIdWF = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(factura.Identificador));
                            if (resultIdWF.Mensaje != "null")
                            {
                                var avance = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(factura.Identificador), JsonConfig.IdSalidaWorkFlowTareaPendienteEnvioAERP, int.Parse(resultIdWF.Mensaje), true);
                                if (!avance.Resultado)
                                {
                                    log.Error("GetFacturasSolpheo - Error al avanzar Workflow tras enviar documento a ERP con IdFileItem " + factura.Identificador);
                                }
                            }

                            Facturas.Add(factura);
                        }
                        catch (Exception ex)
                        {
                            log.Error("GetFacturasSolpheo - Error Generando XML - " + ex.Message, ex);
                        }
                    }
                }


                //Recuperamos todos los XML de la carpeta de salida
                string RutaAccesoSalidaERP = JsonConfig.XMLRutaSalidaERP;
                string IdentificadorRespuesta = "";
                string Estado = "";
                if (!Directory.Exists(RutaAccesoSalidaERP))
                {
                    log.Error("GetFacturasSolpheo - Ruta Acceso Salida ERP " + RutaAccesoSalidaERP + " no existe");
                }
                else
                {
                    string[] allfiles = Directory.GetFiles(RutaAccesoSalidaERP, "*.XML", SearchOption.TopDirectoryOnly);
                    foreach (var file in allfiles)
                    {
                        try
                        {
                            FileInfo info = new FileInfo(file);

                            log.Information($"GetFacturasSolpheo - Procesando fichero {info.Name}");

                            IdentificadorRespuesta = info.Name.Substring(0, info.Name.Length - 4);
                            var respuestaMetadatosEstado = await clienteSolpheo.MetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), int.Parse(IdentificadorRespuesta));
                            idMetadatoArchivadorEstado = int.Parse(JsonConfig.IdMetadataArchivadorFacturasEstado);
                            Estado = respuestaMetadatosEstado.Items.Where(m => m.IdMetadata == idMetadatoArchivadorEstado).FirstOrDefault().StringValue;
                            if (Estado == JsonConfig.EstadoFacturaPendienteContabilizada || Estado == JsonConfig.EstadoFacturaContabilizadaOK)
                            {
                                string Comentario = "";
                                string LibreFechaDia = "";
                                string LibreFechaMes = "";
                                string LibreFechaAno = "";
                                string LibreLista = "";
                                string Fichero = "";
                                var filenameSalida = RutaAccesoSalidaERP + "\\" + IdentificadorRespuesta + ".XML";
                                if (File.Exists(filenameSalida))
                                {
                                    XmlDocument xDoc = new XmlDocument();
                                    xDoc.Load(filenameSalida);

                                    XmlNodeList elemList = xDoc.GetElementsByTagName("Fichero");
                                    for (int a = 0; a < elemList.Count; a++)
                                    {
                                        Fichero = elemList[a].InnerXml;
                                        if (Fichero == IdentificadorRespuesta)
                                        {
                                            XmlNodeList elemListComentario = xDoc.GetElementsByTagName("Comentarios");
                                            Comentario = elemListComentario[a].InnerText;
                                            XmlNodeList elemListLibreFechaDia = xDoc.GetElementsByTagName("LibreFecha");
                                            LibreFechaDia = elemListLibreFechaDia[a].ChildNodes[0].InnerText;
                                            LibreFechaMes = elemListLibreFechaDia[a].ChildNodes[1].InnerText;
                                            LibreFechaAno = elemListLibreFechaDia[a].ChildNodes[2].InnerText;
                                            XmlNodeList elemListLibreLista = xDoc.GetElementsByTagName("LibreLista");
                                            LibreLista = elemListLibreLista[a].InnerText;
                                            XmlNodeList elemListFichero = xDoc.GetElementsByTagName("Fichero");
                                            Fichero = elemListFichero[a].InnerText;
                                        }
                                    }
                                    if (Fichero == IdentificadorRespuesta)
                                    {
                                        if (Comentario.ToUpper() == "RECHAZADA")
                                        {
                                            //Actualizamos las variables del WF al estado "Rechazada"
                                            var resultIdWFSalida = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta));
                                            var avancesalida = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), JsonConfig.IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoRechazada, int.Parse(resultIdWFSalida.Mensaje), true);
                                            if (!avancesalida.Resultado)
                                            {
                                                //si da error se informa en el log y se mueve el XML a una subcarpeta KO
                                                log.Error("Avanzar Workflow - Error al avanzar Workflow a estado Rechazada para el IdFileItem " + IdentificadorRespuesta);
                                                string path = filenameSalida;
                                                string directoriosalidacopiado = RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_KO\";
                                                string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
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
                                                string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
                                                if (!Directory.Exists(directoriosalidacopiado))
                                                {
                                                    Directory.CreateDirectory(directoriosalidacopiado);
                                                }
                                                File.Move(path, ficherosalidacopiado);
                                            }
                                        }
                                        else if (Comentario.ToUpper() == "ACEPTADA")
                                        {
                                            bool variableSalidaLLResultado = false;
                                            bool variableSalidaFechaResultado = false;
                                            //actualizamos las variables del WF
                                            var resultIdWorkflow = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta));
                                            if (!resultIdWorkflow.Resultado)
                                            {
                                                log.Error("Modificar variable solpheo - No se ha podido obtener el id del workwlowactivity en Solpheo");
                                            }
                                            else
                                            {
                                                string FechaContable = "";
                                                var metadatas = new List<FileContainerMetadataValue>();

                                                var metadata = new FileContainerMetadataValue();
                                                metadata.IdFileItem = int.Parse(IdentificadorRespuesta);
                                                metadata.IdMetadata = int.Parse(JsonConfig.IdMetadataArchivadorFacturasNumeroAsientoContable);
                                                metadata.StringValue = LibreLista;
                                                metadatas.Add(metadata);
                                                //Actualizamos el metadato Libre Lista
                                                var result = await clienteSolpheo.UpdateMetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), metadatas.ToArray());

                                                var metadatas2 = new List<FileContainerMetadataValue>();
                                                var metadata2 = new FileContainerMetadataValue();
                                                metadata2.IdFileItem = int.Parse(IdentificadorRespuesta);
                                                metadata2.IdMetadata = int.Parse(JsonConfig.IdMetadataArchivadorFacturasFechaRegistroContable);

                                                if (LibreFechaDia != "")
                                                    FechaContable = LibreFechaDia + "/" + LibreFechaMes + "/" + LibreFechaAno;

                                                metadata2.DateTimeValue = DateTime.Parse(FechaContable);
                                                metadatas2.Add(metadata2);
                                                //Actualizamos el metadato Fecha Contable
                                                var result2 = await clienteSolpheo.UpdateMetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), metadatas2.ToArray());

                                                if (result.Resultado && result2.Resultado)
                                                {
                                                    var idWFActivity = int.Parse(resultIdWorkflow.Mensaje);
                                                    var variableUpdatedLista = await clienteSolpheo.UpdateVariablesWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), int.Parse(JsonConfig.IdMetadataArchivadorFacturasNumeroAsientoContable), "Número de asiento contable", "StringValue", LibreLista, idWFActivity);
                                                    variableSalidaLLResultado = variableUpdatedLista.Resultado;
                                                    if (!variableUpdatedLista.Resultado)
                                                    {
                                                        log.Error("Modificar variable solpheo - No se ha podido modificar la variable de Solpheo NúmeroAsientoContable para el idfileitem " + IdentificadorRespuesta);
                                                    }
                                                    if (FechaContable != "")
                                                    {
                                                        var variableUpdatedFecha = await clienteSolpheo.UpdateVariablesWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), int.Parse(JsonConfig.IdMetadataArchivadorFacturasFechaRegistroContable), "Fecha Registro Contable", "DateTimeValue", FechaContable, idWFActivity);
                                                        variableSalidaFechaResultado = variableUpdatedFecha.Resultado;
                                                        if (!variableUpdatedFecha.Resultado)
                                                        {
                                                            log.Error("Modificar variable solpheo - No se ha podido modificar la variable de Solpheo FechaRegistroContable para el idfileitem " + IdentificadorRespuesta);
                                                        }
                                                    }
                                                    else
                                                        variableSalidaFechaResultado = true;
                                                }
                                            }
                                            //si da error, mostrar mensaje y mover XML
                                            if (!variableSalidaFechaResultado || !variableSalidaLLResultado)
                                            {
                                                log.Error("Avanzar Workflow - Error al actualizar la fecharegistrocontable o número de asiento tras recibir XML con resultado contabilización del ERP con idfileitem " + IdentificadorRespuesta);
                                                string path = filenameSalida;
                                                string directoriosalidacopiado = RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_KO\";
                                                string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
                                                if (!Directory.Exists(directoriosalidacopiado))
                                                {
                                                    Directory.CreateDirectory(directoriosalidacopiado);
                                                }
                                                File.Move(path, ficherosalidacopiado);
                                            }
                                            //Avanzamos el WF al estado = aceptada
                                            var resultIdWFSalida = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta));
                                            var avancesalida = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), JsonConfig.IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoAceptada, int.Parse(resultIdWFSalida.Mensaje), true);
                                            if (!avancesalida.Resultado)
                                            {
                                                //si da error se informa en el log y se mueve el XML a una subcarpeta KO
                                                log.Error("Avanzar Workflow - Error al avanzar Workflow tras recibir XML con resultado contabilización del ERP para el IdFileItem " + IdentificadorRespuesta);
                                                string path = filenameSalida;
                                                string directoriosalidacopiado = RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_KO\";
                                                string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
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
                                                string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
                                                if (!Directory.Exists(directoriosalidacopiado))
                                                {
                                                    Directory.CreateDirectory(directoriosalidacopiado);
                                                }
                                                File.Move(path, ficherosalidacopiado);
                                            }
                                        }
                                        else if (Comentario.ToUpper() == "PAGADA")
                                        {
                                            //Actualizamos el WF al estado "Pagada"
                                            var resultIdWFSalida = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta));
                                            var avancesalida = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), JsonConfig.IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoPagada, int.Parse(resultIdWFSalida.Mensaje), true);
                                            if (!avancesalida.Resultado)
                                            {
                                                //si da error se informa en el log y se mueve el XML a una subcarpeta KO
                                                log.Error("Avanzar Workflow - Error al avanzar Workflow a estado Pagada para el IdFileItem " + IdentificadorRespuesta);
                                                string path = filenameSalida;
                                                string directoriosalidacopiado = RutaAccesoSalidaERP + @"PAGO_PROCESADO_KO\";
                                                string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
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
                                                string directoriosalidacopiado = RutaAccesoSalidaERP + @"PAGO_PROCESADO_OK\";
                                                string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
                                                if (!Directory.Exists(directoriosalidacopiado))
                                                {
                                                    Directory.CreateDirectory(directoriosalidacopiado);
                                                }
                                                File.Move(path, ficherosalidacopiado);
                                            }
                                        }
                                        else
                                        {
                                            log.Error("Obtener XML Salida - El documento " + IdentificadorRespuesta + " no se encuentra ni Contabilizado ni Rechazado");
                                            filenameSalida = RutaAccesoSalidaERP + IdentificadorRespuesta + ".XML";
                                            string path = filenameSalida;
                                            string directoriosalidacopiado = RutaAccesoSalidaERP + @"XML_FORMATO_INCORRECTO\";
                                            string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
                                            if (!Directory.Exists(directoriosalidacopiado))
                                            {
                                                Directory.CreateDirectory(directoriosalidacopiado);
                                            }
                                            File.Move(path, ficherosalidacopiado);
                                        }
                                    }
                                    else
                                    {
                                        log.Error("Obtener XML Salida - El documento " + IdentificadorRespuesta + " no se corresponde con el campo Fichero delntro del XML");
                                        filenameSalida = RutaAccesoSalidaERP + IdentificadorRespuesta + ".XML";
                                        string path = filenameSalida;
                                        string directoriosalidacopiado = RutaAccesoSalidaERP + @"XML_FORMATO_INCORRECTO\";
                                        string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
                                        if (!Directory.Exists(directoriosalidacopiado))
                                        {
                                            Directory.CreateDirectory(directoriosalidacopiado);
                                        }
                                        File.Move(path, ficherosalidacopiado);
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                log.Error("Obtener XML Salida - El documento " + IdentificadorRespuesta + " no se encuentra en estado Pendiente Respuesta ERP");
                                //si el XML de salida no se encuentra en estado Pendiente Respuesta ERP, se mueve en la subcarpeta XML_FORMATO_INCORRECTO
                                var filenameSalida = RutaAccesoSalidaERP + IdentificadorRespuesta + ".XML";
                                string path = filenameSalida;
                                string directoriosalidacopiado = RutaAccesoSalidaERP + @"XML_FORMATO_INCORRECTO\";
                                string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
                                if (!Directory.Exists(directoriosalidacopiado))
                                {
                                    Directory.CreateDirectory(directoriosalidacopiado);
                                }
                                File.Move(path, ficherosalidacopiado);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("Obtener XML Salida - El documento " + IdentificadorRespuesta + " ha dado el siguiente error: " + ex.Message, ex);

                            var filenameSalida = RutaAccesoSalidaERP + IdentificadorRespuesta + ".XML";
                            string path = filenameSalida;
                            string directoriosalidacopiado = RutaAccesoSalidaERP + @"XML_FORMATO_INCORRECTO\";
                            string ficherosalidacopiado = directoriosalidacopiado + IdentificadorRespuesta + ".XML";
                            if (!Directory.Exists(directoriosalidacopiado))
                            {
                                Directory.CreateDirectory(directoriosalidacopiado);
                            }
                            File.Move(path, ficherosalidacopiado);
                        }



                    }
                }

                //Carga de codigos de obra en el portal de proveedores
                await FicheroCSV_CodigoObra(JsonConfig);

                //Carga de proveedores en el portal de proveedores
                await FicheroCSV_Proveedores(JsonConfig);

                log.Information("GetFacturasSolpheo - Fin método");
            }

            catch (Exception ex)
            {
                log.Error("GetFacturasSolpheo - Error General - " + ex.Message, ex);
                resultadoOK = false;
            }

            if (!String.IsNullOrEmpty(Error))
            {
                response.DescripcionError = Error;
                response.Estado = "KO";
            }

            log.Information("------------------ Fin vuelta Servicio ---------------------", SolpheoIdTenant);
            log.Information("", SolpheoIdTenant);

            return resultadoOK;
        }

        private async Task<XmlDocument> GenerarXMLFactura(ClienteSolpheo clienteSolpheo, Login loginSolpheo, Configuracion JsonConfig, Factura factura, List<FileContainerListViewModel> FileItemsRegistro)
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
            XmlText DiaFE = doc.CreateTextNode(factura.FechaEmision.Day.ToString().PadLeft(2, '0'));
            FechaEmisionDia.AppendChild(DiaFE);
            FechaEmision.AppendChild(FechaEmisionDia);
            XmlElement FechaEmisionMes = doc.CreateElement(string.Empty, "Mes", string.Empty);
            XmlText MesFE = doc.CreateTextNode(factura.FechaEmision.Month.ToString().PadLeft(2, '0'));
            FechaEmisionMes.AppendChild(MesFE);
            FechaEmision.AppendChild(FechaEmisionMes);
            XmlElement FechaEmisionAno = doc.CreateElement(string.Empty, "Ano", string.Empty);
            XmlText AnoFE = doc.CreateTextNode(factura.FechaEmision.Year.ToString());
            FechaEmisionAno.AppendChild(AnoFE);
            FechaEmision.AppendChild(FechaEmisionAno);
            XmlElement FechaRecepcion = doc.CreateElement(string.Empty, "FechaRecepcion", string.Empty);
            fact.AppendChild(FechaRecepcion);
            XmlElement FechaRecepcionDia = doc.CreateElement(string.Empty, "Dia", string.Empty);
            XmlText DiaFR = doc.CreateTextNode(factura.FechaRecepcion.Day.ToString().PadLeft(2, '0'));
            FechaRecepcionDia.AppendChild(DiaFR);
            FechaRecepcion.AppendChild(FechaRecepcionDia);
            XmlElement FechaRecepcionMes = doc.CreateElement(string.Empty, "Mes", string.Empty);
            XmlText MesFR = doc.CreateTextNode(factura.FechaRecepcion.Month.ToString().PadLeft(2, '0'));
            FechaRecepcionMes.AppendChild(MesFR);
            FechaRecepcion.AppendChild(FechaRecepcionMes);
            XmlElement FechaRecepcionAno = doc.CreateElement(string.Empty, "Ano", string.Empty);
            XmlText AnoFR = doc.CreateTextNode(factura.FechaRecepcion.Year.ToString());
            FechaRecepcionAno.AppendChild(AnoFR);
            FechaRecepcion.AppendChild(FechaRecepcionAno);
            XmlElement totfact = doc.CreateElement(string.Empty, "Total", string.Empty);
            XmlText totfactTexto = doc.CreateTextNode(factura.TotalFactura.ToString().Replace(",", "."));
            totfact.AppendChild(totfactTexto);
            fact.AppendChild(totfact);
            XmlElement concepto = doc.CreateElement(string.Empty, "Concepto", string.Empty);
            XmlText conceptotexto = doc.CreateTextNode(factura.Comentarios);
            concepto.AppendChild(conceptotexto);
            fact.AppendChild(concepto);
            XmlElement fichero = doc.CreateElement(string.Empty, "Fichero", string.Empty);
            XmlText fichTexto = doc.CreateTextNode(factura.Identificador);
            fichero.AppendChild(fichTexto);
            fact.AppendChild(fichero);
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
                XmlText baseTexto = doc.CreateTextNode(factura.BaseSujeta.Replace(",", "."));
                Base.AppendChild(baseTexto);
                Impuesto.AppendChild(Base);
                XmlElement Tipo = doc.CreateElement(string.Empty, "Tipo", string.Empty);
                XmlText tipoTexto = doc.CreateTextNode(factura.TipoIVA.Replace(",", "."));
                Tipo.AppendChild(tipoTexto);
                Impuesto.AppendChild(Tipo);
                XmlElement CuotaIVA = doc.CreateElement(string.Empty, "CuotaIVA", string.Empty);
                XmlText cuotaIVATexto = doc.CreateTextNode(factura.CuotaIVA.Replace(",", "."));
                CuotaIVA.AppendChild(cuotaIVATexto);
                Impuesto.AppendChild(CuotaIVA);
                XmlElement RecEq = doc.CreateElement(string.Empty, "RecEq", string.Empty);
                XmlText recEqTexto = doc.CreateTextNode(factura.RecEq.Replace(",", "."));
                RecEq.AppendChild(recEqTexto);
                Impuesto.AppendChild(RecEq);
                XmlElement CuotaReq = doc.CreateElement(string.Empty, "CuotaReq", string.Empty);
                XmlText cuotaReqTexto = doc.CreateTextNode(factura.CuotaReq.Replace(",", "."));
                CuotaReq.AppendChild(cuotaReqTexto);
                Impuesto.AppendChild(CuotaReq);
            }


            XmlElement LibreNumero = doc.CreateElement(string.Empty, "LibreNumero", string.Empty);
            fact.AppendChild(LibreNumero);
            XmlElement LibreTexto = doc.CreateElement(string.Empty, "LibreTexto", string.Empty);
            XmlText libreTextoTexto = doc.CreateTextNode("CodObra:" + factura.CodigoObra + " - NumPedido:" + factura.NumeroPedido);
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
            XmlText comentarioTexto = doc.CreateTextNode(factura.RutaDescarga + "/Documento/BuscarDocumento?IdFileContainer=" + JsonConfig.IdFileContainerArchivadorFacturas + "&IdDocumento=" + factura.Identificador);
            comentario.AppendChild(comentarioTexto);
            fact.AppendChild(comentario);

            return doc;
        }

        public async Task<bool> FicheroCSV_CodigoObra(Configuracion JsonConfig)
        {
            if (!Directory.Exists(JsonConfig.CSVCodigosObraRutaERP))
            {
                log.Information($"FicheroCSV_CodigoObra - No existe el directorio de salida de este CSV -> {JsonConfig.CSVCodigosObraRutaERP}");
            }
            else
            {
                string[] allfiles = Directory.GetFiles(JsonConfig.CSVCodigosObraRutaERP, "*.CSV", SearchOption.TopDirectoryOnly);
                foreach (var file in allfiles)
                {
                    try
                    {
                        await BorrarCodigosObras(JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);

                        FileInfo info = new FileInfo(file);

                        log.Information($"FicheroCSV_CodigoObra - Procesando fichero {info.Name}");

                        bool resultadoFicheroOK = true;                        

                        using (StreamReader sr = new StreamReader(file))
                        {
                            
                            int numLinea = 0;

                            while (sr.Peek() >= 0)
                            {
                                numLinea++;
                                bool resultadoLineaOK = true;
                                string datosLinea = "";

                                try
                                {
                                    datosLinea = sr.ReadLine();

                                    if (!string.IsNullOrEmpty(datosLinea))
                                    {
                                        resultadoLineaOK = await GrabaCodigoObra(datosLinea, JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);
                                    }                                    
                                }

                                catch (Exception ex)
                                {
                                    resultadoLineaOK = false;
                                }
                                finally
                                {
                                    if (!resultadoLineaOK)
                                    {
                                        resultadoFicheroOK = false;
                                        log.Information($"FicheroCSV_CodigoObra - Procesando fichero {info.Name} - Error de la API al grabar el código obra {datosLinea} en la fial {numLinea} del fichero");
                                    }
                                }
                            }
                        }

                        string directorioSalida = "";

                        if (resultadoFicheroOK)
                        {
                            directorioSalida = JsonConfig.CSVCodigosObraRutaERP + @"\PROCESADO_OK\";
                        }
                        else
                        {
                            directorioSalida = JsonConfig.CSVCodigosObraRutaERP + @"\PROCESADO_KO\";
                        }

                        if (!Directory.Exists(directorioSalida))
                        {
                            Directory.CreateDirectory(directorioSalida);
                        }                        

                        File.Move(file, directorioSalida + "\\" + info.Name.Replace(".csv", "") + "_" + Guid.NewGuid().ToString() + ".csv");

                    }
                    catch (Exception ex)
                    {
                        log.Error($"FicheroCSV_CodigoObra - Error general procesando fichero '{file}' - {ex.Message}", ex);
                    }
                }

            }

            return true;
        }

        public async Task<bool> BorrarCodigosObras(string url, string ApiKey)
        {
            bool resultadoOK = true;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("ApiKey", ApiKey);

                    var responseClient = await client.PostAsync(new Uri(url + $"/ePortalProveedores/BorrarCodigosObra"), null);
                    if (!responseClient.IsSuccessStatusCode)
                    {
                        log.Information($"BorrarCodigosObras - Error en llamada al API del portal de proveedores para borrar los códigos de obras ");
                        resultadoOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"BorrarCodigosObras - Error en llamada al API del portal de proveedores para borrar los códigos de obras - {ex.Message}");
                resultadoOK = false;
            }

            return resultadoOK;
        }

        public async Task<bool> GrabaCodigoObra(string codObra, string url, string ApiKey)
        {
            bool resultadoOK = true;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("ApiKey", ApiKey);                    

                    var responseClient = await client.PostAsync(new Uri(url + $"/ePortalProveedores/AltaCodigoObra/CodigoObra/{codObra}"), null);
                    if (!responseClient.IsSuccessStatusCode)
                    {
                        log.Information($"Grabar Codigo Obra - Error en llamada al API del portal de proveedores para codigo de obra {codObra}");
                        resultadoOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Grabar Codigo Obra - Error en llamada al API del portal de proveedores para codigo de obra {codObra} - {ex.Message}");
                resultadoOK = false;
            }

            return resultadoOK;
        }

        public async Task<bool> FicheroCSV_Proveedores(Configuracion JsonConfig)
        {
            if (!Directory.Exists(JsonConfig.CSVProveedoresRutaERP))
            {
                log.Information($"FicheroCSV_Proveedores - No existe el directorio de salida de este CSV -> {JsonConfig.CSVProveedoresRutaERP}");
                return false;
            }

            string[] allfiles = Directory.GetFiles(JsonConfig.CSVProveedoresRutaERP, "*.CSV", SearchOption.TopDirectoryOnly);
            foreach (var file in allfiles)
            {
                try
                {
                    FileInfo info = new FileInfo(file);

                    log.Information($"FicheroCSV_Proveedores - Procesando fichero {info.Name}");                    

                    bool resultadoFicheroOK = true;

                    using (StreamReader sr = new StreamReader(file))
                    {

                        int numLinea = 0;

                        while (sr.Peek() >= 0)
                        {
                            numLinea++;
                            bool resultadoLineaOK = true;
                            string datosLinea = "";

                            try
                            {
                                datosLinea = sr.ReadLine();

                                if (!string.IsNullOrEmpty(datosLinea))
                                {
                                    var datosProveedor = datosLinea.Split(';');

                                    if (string.IsNullOrEmpty(datosProveedor[0]) || string.IsNullOrEmpty(datosProveedor[1]) || string.IsNullOrEmpty(datosProveedor[2]) || datosProveedor[2] == "#")
                                    {
                                        log.Information($"FicheroCSV_Proveedores - Procesando fichero {info.Name} - Error al leer la fila {numLinea} del fichero. Datos incompletos");
                                    }
                                    else
                                    {
                                        var emails = datosProveedor[2].Split('#');

                                        foreach(string email in emails)
                                        {
                                            if (!string.IsNullOrEmpty(email))
                                            {
                                                var resultadoEmail = await GrabaProveedor(datosProveedor[0], datosProveedor[1], email, datosProveedor[3], JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);

                                                if (!resultadoEmail) { resultadoLineaOK = false; }
                                            }                                            

                                        }
                                        
                                    }
                                }

                                
                            }

                            catch (Exception ex)
                            {
                                resultadoLineaOK = false;
                            }
                            finally
                            {
                                if (!resultadoLineaOK)
                                {
                                    resultadoFicheroOK = false;
                                    log.Information($"FicheroCSV_Proveedores - Procesando fichero {info.Name} - Error de la API al grabar la fila {numLinea} del fichero");
                                }
                            }
                        }
                    }

                    string directorioSalida = "";

                    if (resultadoFicheroOK)
                    {
                        directorioSalida = JsonConfig.CSVProveedoresRutaERP + @"\PROCESADO_OK\";
                    }
                    else
                    {
                        directorioSalida = JsonConfig.CSVProveedoresRutaERP + @"\PROCESADO_KO\";
                    }

                    if (!Directory.Exists(directorioSalida))
                    {
                        Directory.CreateDirectory(directorioSalida);
                    }
                    File.Move(file, directorioSalida + "\\" + info.Name.Replace(".csv", "") + "_" + Guid.NewGuid().ToString() + ".csv");



                }
                catch (Exception ex)
                {
                    log.Error($"FicheroCSV_Proveedores - Error general procesando fichero '{file}' - {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<bool> GrabaProveedor(string CIF, string Nombre, string Email, string fechaReenvioEmailBienvenida, string url, string ApiKey)
        {
            bool resultadoOK = true;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("ApiKey", ApiKey);                    

                    var responseClient = await client.PostAsync(new Uri(url + $"/ePortalProveedores/AltaProveedores/Nombre/{Nombre}/NIF/{CIF}/Email/{Email}/CodigoPerfil/PROV"), null);
                    if (!responseClient.IsSuccessStatusCode)
                    {
                        log.Information($"Grabar Proveedores - Resultado no OK en llamada al API del portal de proveedores para proveedor con nombre {Nombre}, CIF {CIF} y Email {Email}");
                        resultadoOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Grabar Proveedores - Error general en llamada al API del portal de proveedores para proveedor con nombre {Nombre}, CIF {CIF} y Email {Email} - {ex.Message}");
                resultadoOK = false;
            }

            return resultadoOK;
        }

    }
}
