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
using System.Text;

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
        int idMetadatoArchivadorComentarios = 0;
        int idMetadatoArchivadorRazonSocialProveedor = 0;

        int idMetadatoArchivadorImpuesto1Base = 0;
        int idMetadatoArchivadorImpuesto1IVATipo = 0;
        int idMetadatoArchivadorImpuesto1IVACuota = 0;
        int idMetadatoArchivadorImpuesto1RecargoEquivalenciaTipo = 0;
        int idMetadatoArchivadorImpuesto1RecargoEquivalenciaCuota = 0;
        int idMetadatoArchivadorImpuesto2Base = 0;
        int idMetadatoArchivadorImpuesto2IVATipo = 0;
        int idMetadatoArchivadorImpuesto2IVACuota = 0;
        int idMetadatoArchivadorImpuesto2RecargoEquivalenciaTipo = 0;
        int idMetadatoArchivadorImpuesto2RecargoEquivalenciaCuota = 0;
        int idMetadatoArchivadorImpuesto3Base = 0;
        int idMetadatoArchivadorImpuesto3IVATipo = 0;
        int idMetadatoArchivadorImpuesto3IVACuota = 0;
        int idMetadatoArchivadorImpuesto3RecargoEquivalenciaTipo = 0;
        int idMetadatoArchivadorImpuesto3RecargoEquivalenciaCuota = 0;
        int idMetadatoArchivadorImpuesto4Base = 0;
        int idMetadatoArchivadorImpuesto4IVATipo = 0;
        int idMetadatoArchivadorImpuesto4IVACuota = 0;
        int idMetadatoArchivadorImpuesto4RecargoEquivalenciaTipo = 0;
        int idMetadatoArchivadorImpuesto4RecargoEquivalenciaCuota = 0;


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

            ClienteSolpheo clienteSolpheo = new ClienteSolpheo(JsonConfig.SolpheoUrl);
            Login loginSolpheo = new Login();

            //Procesamiento facturas para las que generar XML y enviar a carpeta de entrada del ERP
            try
            {
                loginSolpheo = await clienteSolpheo.LoginAsync(JsonConfig.SolpheoUsuario, JsonConfig.SolpheoPassword, JsonConfig.SolpheoTenant, "multifuncional", "MfpSecret", "api");

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
                        await ProcesarFacturaAEnviarAERP(FileItems[i].Id.ToString(), clienteSolpheo, loginSolpheo, jsonConfig);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("GetFacturasSolpheo - Error General - " + ex.Message, ex);
                resultadoOK = false;
            }


            //Procesamiento XML en carpeta de salida del ERP
            try
            {
                string RutaAccesoSalidaERP = JsonConfig.XMLRutaSalidaERP;
                string IdentificadorRespuesta = "";

                if (!Directory.Exists(RutaAccesoSalidaERP))
                {
                    log.Error("Procesado XMLs Salida - Ruta Acceso Salida ERP " + RutaAccesoSalidaERP + " no existe");
                    return false;
                }

                Directory.CreateDirectory(RutaAccesoSalidaERP + @"\XML_FORMATO_INCORRECTO\");
                Directory.CreateDirectory(RutaAccesoSalidaERP + @"\KO_DOC_SIN_WORKFLOW\");
                Directory.CreateDirectory(RutaAccesoSalidaERP + @"\PAGOAPROBADO_PROCESADO_OK\");
                Directory.CreateDirectory(RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_OK\");
                Directory.CreateDirectory(RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_KO\");

                string[] allfiles = Directory.GetFiles(RutaAccesoSalidaERP, "*.XML", SearchOption.TopDirectoryOnly);
                Array.Sort(allfiles, StringComparer.InvariantCulture);

                foreach (var file in allfiles)
                {
                    FileInfo info = new FileInfo(file);

                    try
                    {                        
                        log.Information("Procesado XMLs Salida - Documento {0}", info.Name);                        
                        
                        IdentificadorRespuesta = info.Name.Substring(0, info.Name.IndexOf(".xml"));
                        IdentificadorRespuesta = IdentificadorRespuesta.Replace("_1", "");
                        IdentificadorRespuesta = IdentificadorRespuesta.Replace("_2", "");


                        var fileItem = await clienteSolpheo.FileItemsByIdAsync(
                            loginSolpheo.AccessToken,
                            int.Parse(JsonConfig.IdFileContainerArchivadorFacturas),
                            int.Parse(IdentificadorRespuesta));

                        if (fileItem == null)
                        {
                            log.Error("Procesado XMLs Salida - Documento {0} - Error obteniendo estado del fichero", IdentificadorRespuesta);
                            continue;
                        }

                        if (fileItem.State != 8) // es decir, no está en workflow
                        {
                            log.Error("Procesado XMLs Salida - Documento {0} - El documento ya no está en Workflow", IdentificadorRespuesta);
                            MoverFicheroADirectorioDestino(RutaAccesoSalidaERP, RutaAccesoSalidaERP + @"\KO_DOC_SIN_WORKFLOW\", info.Name, IdentificadorRespuesta);
                            continue;
                        }



                        var filenameSalida = RutaAccesoSalidaERP + "\\" + info.Name;
                        FacturaObjetoXML xmlFactura = GenerarObjetoXMLFactura(filenameSalida, IdentificadorRespuesta);

                        if (xmlFactura == null)
                        {
                            log.Error("Procesado XMLs Salida - Documento {0} - Error leyendo XML {1}", IdentificadorRespuesta);
                            MoverFicheroADirectorioDestino(RutaAccesoSalidaERP, RutaAccesoSalidaERP + @"\XML_FORMATO_INCORRECTO\", info.Name, IdentificadorRespuesta);
                            continue;
                        }



                        var resultIdWFSalida = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta));

                        if (!resultIdWFSalida.Resultado)
                        {
                            log.Error("Procesado XMLs Salida - Documento {0} - Error en la llamada al API para obtener Id Actividad. Se reintentará en próxima ejecución", IdentificadorRespuesta);
                            continue;
                        }


                        if (((xmlFactura.EstadoXML == "ACEPTADA" || xmlFactura.EstadoXML == "RECHAZADA") && resultIdWFSalida.TaskKey != JsonConfig.TaskKeyTareaPendienteContabilizacionERP && resultIdWFSalida.TaskKey != JsonConfig.TaskKeyTareaPendienteAprobacionPagoERP)
                            || (xmlFactura.EstadoXML == "PAGOAPROBADO" && resultIdWFSalida.TaskKey != JsonConfig.TaskKeyTareaPendienteAprobacionPagoERP))
                        {
                            log.Error("Procesado XMLs Salida - Documento {0} - En el XML aparece el estado {1} pero el workflow aún no se encuentra en la tarea apropiada (está en la que tiene TaskKey {2}. Se reintentará en próxima ejecución", IdentificadorRespuesta, xmlFactura.EstadoXML, resultIdWFSalida.TaskKey);
                            continue;
                        }

                        if ((xmlFactura.EstadoXML == "ACEPTADA" || xmlFactura.EstadoXML == "RECHAZADA") && resultIdWFSalida.TaskKey == JsonConfig.TaskKeyTareaPendienteAprobacionPagoERP)
                        {
                            log.Error("Procesado XMLs Salida - Documento {0} - En el XML aparece el estado {1} pero el workflow se encuentra en la tarea con TaskKey {2} y por tanto ya se está a la espera de aprobación de pago. ", IdentificadorRespuesta, xmlFactura.EstadoXML,resultIdWFSalida.TaskKey);
                            MoverFicheroADirectorioDestino(RutaAccesoSalidaERP, RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_KO\", info.Name, IdentificadorRespuesta);
                            continue;
                        }

                        if (xmlFactura.EstadoXML == "RECHAZADA")
                        {
                            var resultado = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), JsonConfig.IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoRechazada, int.Parse(resultIdWFSalida.Id), true);
                            if (!resultado.Resultado)
                            {
                                log.Error("Procesado XMLs Salida - Documento {0} - Error avanzando el Workflow tras recibir en el XML aparece el estado {2}. Se reintentará en próxima ejecución", IdentificadorRespuesta, xmlFactura.EstadoXML);
                                continue;
                            }

                            log.Information("Procesado XMLs Salida - Documento {0} - Procesado OK", IdentificadorRespuesta);
                            MoverFicheroADirectorioDestino(RutaAccesoSalidaERP, RutaAccesoSalidaERP + @"\CONTABILIZACION_PROCESADA_OK\", info.Name, IdentificadorRespuesta);
                            continue;

                        }

                        if (xmlFactura.EstadoXML == "ACEPTADA")
                        {
                            string FechaContable = "";
                            var metadatas = new List<FileContainerMetadataValue>();

                            var metadata = new FileContainerMetadataValue();
                            metadata.IdFileItem = int.Parse(IdentificadorRespuesta);
                            metadata.IdMetadata = int.Parse(JsonConfig.IdMetadataArchivadorFacturasNumeroAsientoContable);
                            metadata.StringValue = xmlFactura.LibreLista;
                            metadatas.Add(metadata);
                            var resultNumAsiento = await clienteSolpheo.UpdateMetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), metadatas.ToArray());

                            metadata = new FileContainerMetadataValue();
                            metadata.IdFileItem = int.Parse(IdentificadorRespuesta);
                            metadata.IdMetadata = int.Parse(JsonConfig.IdMetadataArchivadorFacturasFechaRegistroContable);
                            FechaContable = xmlFactura.LibreFechaDia + "/" + xmlFactura.LibreFechaMes + "/" + xmlFactura.LibreFechaAno;
                            metadata.DateTimeValue = DateTime.Parse(FechaContable);
                            metadatas.Add(metadata);
                            var resultFechaRegistroContable = await clienteSolpheo.UpdateMetadatasFileItemAsync(loginSolpheo.AccessToken, int.Parse(JsonConfig.IdFileContainerArchivadorFacturas), metadatas.ToArray());

                            if (!resultFechaRegistroContable.Resultado || !resultNumAsiento.Resultado)
                            {
                                log.Error("Procesado XMLs Salida - Documento {0} - Error actualizando metadatos tras recibir en el XML aparece el estado {2}. Se reintentará en próxima ejecución", IdentificadorRespuesta, xmlFactura.EstadoXML);
                                continue;
                            }

                            resultNumAsiento = await clienteSolpheo.UpdateVariablesWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), int.Parse(JsonConfig.IdMetadataArchivadorFacturasNumeroAsientoContable), "Número de asiento contable", "StringValue", xmlFactura.LibreLista, int.Parse(resultIdWFSalida.Id));
                            resultFechaRegistroContable = await clienteSolpheo.UpdateVariablesWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), int.Parse(JsonConfig.IdMetadataArchivadorFacturasFechaRegistroContable), "Fecha Registro Contable", "DateTimeValue", FechaContable, int.Parse(resultIdWFSalida.Id));

                            if (!resultFechaRegistroContable.Resultado || !resultNumAsiento.Resultado)
                            {
                                log.Error("Procesado XMLs Salida - Documento {0} - Error actualizando variables del Workflow tras recibir en el XML aparece el estado {2}. Se reintentará en próxima ejecución", IdentificadorRespuesta, xmlFactura.EstadoXML);
                                continue;
                            }


                            var avancesalida = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), JsonConfig.IdSalidaWorkFlowTareaPendienteContabilizacionERP_ResultadoAceptada, int.Parse(resultIdWFSalida.Id), true);
                            if (!avancesalida.Resultado)
                            {
                                log.Error("Procesado XMLs Salida - Documento {0} - Error avanzando el Workflow tras recibir en el XML aparece el estado {2}. Se reintentará en próxima ejecución", IdentificadorRespuesta, xmlFactura.EstadoXML);
                                continue;
                            }

                            log.Information("Procesado XMLs Salida - Documento {0} - Procesado OK", IdentificadorRespuesta);
                            MoverFicheroADirectorioDestino(RutaAccesoSalidaERP, RutaAccesoSalidaERP + @"CONTABILIZACION_PROCESADA_OK\", info.Name, IdentificadorRespuesta);

                            continue;

                        }

                        if (xmlFactura.EstadoXML == "PAGOAPROBADO")
                        {
                            var resultado = await clienteSolpheo.AvanzarWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(IdentificadorRespuesta), JsonConfig.IdSalidaWorkFlowTareaPendienteAprobacionPagoERP_ResultadoPagada, int.Parse(resultIdWFSalida.Id), true);
                            if (!resultado.Resultado)
                            {
                                log.Error("Procesado XMLs Salida - Documento {0} - Error avanzando el Workflow tras recibir en el XML aparece el estado {2}. Se reintentará en próxima ejecución", IdentificadorRespuesta, xmlFactura.EstadoXML);
                                continue;
                            }

                            log.Information("Procesado XMLs Salida - Documento {0} - Procesado OK", IdentificadorRespuesta);
                            MoverFicheroADirectorioDestino(RutaAccesoSalidaERP, RutaAccesoSalidaERP + @"\PAGOAPROBADO_PROCESADO_OK\", info.Name, IdentificadorRespuesta);
                            continue;
                        }


                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Procesado XMLs Salida - Documento {0} - Error general {1}. Se reintentará en próxima ejecución", IdentificadorRespuesta, ex.Message);
                    }
                }


            }
            catch (Exception ex)
            {
                log.Error(ex, "Procesado XMLs Salida - Error General - {1}", ex.Message);
                resultadoOK = false;
            }

            //Carga de codigos de obra en el portal de proveedores
            await FicheroCSV_CodigoObra(JsonConfig);

            //Carga de proveedores en el portal de proveedores
            await FicheroCSV_Proveedores(JsonConfig);

            await ActualizarRegistrosSolpheoProveedores(JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);

            await ActualizarRegistrosSolpheoCodigosObra(JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);

            log.Information("GetFacturasSolpheo - Fin método");


            if (!String.IsNullOrEmpty(Error))
            {
                response.DescripcionError = Error;
                response.Estado = "KO";
            }

            log.Information("------------------ Fin vuelta Servicio ---------------------", SolpheoIdTenant);
            log.Information("", SolpheoIdTenant);

            return resultadoOK;
        }

        private void MoverFicheroADirectorioDestino(string directorioOrigen, string directorioDestino, string nombreFichero, string idFileItemFactura)
        {
            try
            {
                string nombreFicheroFinal = Path.GetFileNameWithoutExtension(directorioOrigen + "\\" + nombreFichero);
                nombreFicheroFinal = nombreFichero + "_" + Guid.NewGuid().ToString() + ".xml";
                File.Move(directorioOrigen + "\\" + nombreFichero, directorioDestino + "\\" + nombreFicheroFinal);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Procesado XMLs Salida - Documento {0} - Error moviendo a directorio destino {1} - Error {2}", idFileItemFactura, directorioDestino, ex.Message);
            }
        }

        private FacturaObjetoXML GenerarObjetoXMLFactura(string filenameSalida, string idFileItem)
        {
            FacturaObjetoXML xmlFactura = new FacturaObjetoXML();

            string fichero = "";

            try
            {

                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(filenameSalida);

                XmlNodeList elemList = xDoc.GetElementsByTagName("Fichero");
                for (int a = 0; a < elemList.Count; a++)
                {
                    fichero = elemList[a].InnerXml;
                    if (fichero == idFileItem)
                    {
                        XmlNodeList elemListComentario = xDoc.GetElementsByTagName("Comentarios");
                        xmlFactura.EstadoXML = elemListComentario[a].InnerText;
                        XmlNodeList elemListLibreFechaDia = xDoc.GetElementsByTagName("LibreFecha");
                        xmlFactura.LibreFechaDia = elemListLibreFechaDia[a].ChildNodes[0].InnerText;
                        xmlFactura.LibreFechaMes = elemListLibreFechaDia[a].ChildNodes[1].InnerText;
                        xmlFactura.LibreFechaAno = elemListLibreFechaDia[a].ChildNodes[2].InnerText;
                        XmlNodeList elemListLibreLista = xDoc.GetElementsByTagName("LibreLista");
                        xmlFactura.LibreLista = elemListLibreLista[a].InnerText;
                        XmlNodeList elemListFichero = xDoc.GetElementsByTagName("Fichero");
                        fichero = elemListFichero[a].InnerText;
                    }
                }

                return xmlFactura;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private Factura GenerarObjetoFactura(Configuracion JsonConfig, PagedList<MetadataFileItemValue> respuestaMetadatos)
        {
            Factura factura = new Factura();

            try
            {
                idMetadatoArchivadorCodigoFactura = int.Parse(JsonConfig.IdMetadataArchivadorFacturasCodigoFactura);
                factura.Codigofactura = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorCodigoFactura).FirstOrDefault().StringValue;

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


                idMetadatoArchivadorImpuesto1Base = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto1BaseSujeta);
                factura.Impuesto1Base = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto1Base).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto1IVATipo = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto1IVATipo);
                factura.Impuesto1Tipo = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto1IVATipo).FirstOrDefault().StringValue;

                idMetadatoArchivadorImpuesto1IVACuota = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto1IVACuota);
                factura.Impuesto1Cuota = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto1IVACuota).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto1RecargoEquivalenciaTipo = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto1RecargoEquivalenciaTipo);
                factura.Impuesto1RecEqTipo = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto1RecargoEquivalenciaTipo).FirstOrDefault().StringValue;

                idMetadatoArchivadorImpuesto1RecargoEquivalenciaCuota = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto1RecargoEquivalenciaCuota);
                factura.Impuesto1RecEqCuota = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto1RecargoEquivalenciaCuota).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto2Base = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto2BaseSujeta);
                factura.Impuesto2Base = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto2Base).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto2IVATipo = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto2IVATipo);
                factura.Impuesto2Tipo = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto2IVATipo).FirstOrDefault().StringValue;

                idMetadatoArchivadorImpuesto2IVACuota = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto2IVACuota);
                factura.Impuesto2Cuota = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto2IVACuota).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto2RecargoEquivalenciaTipo = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto2RecargoEquivalenciaTipo);
                factura.Impuesto2RecEqTipo = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto2RecargoEquivalenciaTipo).FirstOrDefault().StringValue;

                idMetadatoArchivadorImpuesto2RecargoEquivalenciaCuota = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto2RecargoEquivalenciaCuota);
                factura.Impuesto2RecEqCuota = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto2RecargoEquivalenciaCuota).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto3Base = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto3BaseSujeta);
                factura.Impuesto3Base = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto3Base).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto3IVATipo = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto3IVATipo);
                factura.Impuesto3Tipo = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto3IVATipo).FirstOrDefault().StringValue;

                idMetadatoArchivadorImpuesto3IVACuota = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto3IVACuota);
                factura.Impuesto3Cuota = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto3IVACuota).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto3RecargoEquivalenciaTipo = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto3RecargoEquivalenciaTipo);
                factura.Impuesto3RecEqTipo = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto3RecargoEquivalenciaTipo).FirstOrDefault().StringValue;

                idMetadatoArchivadorImpuesto3RecargoEquivalenciaCuota = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto3RecargoEquivalenciaCuota);
                factura.Impuesto3RecEqCuota = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto3RecargoEquivalenciaCuota).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto4Base = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto4BaseSujeta);
                factura.Impuesto4Base = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto4Base).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto4IVATipo = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto4IVATipo);
                factura.Impuesto4Tipo = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto4IVATipo).FirstOrDefault().StringValue;

                idMetadatoArchivadorImpuesto4IVACuota = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto4IVACuota);
                factura.Impuesto4Cuota = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto4IVACuota).FirstOrDefault().DecimalValue);

                idMetadatoArchivadorImpuesto4RecargoEquivalenciaTipo = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto4RecargoEquivalenciaTipo);
                factura.Impuesto4RecEqTipo = respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto4RecargoEquivalenciaTipo).FirstOrDefault().StringValue;

                idMetadatoArchivadorImpuesto4RecargoEquivalenciaCuota = int.Parse(JsonConfig.IdMetadataArchivadorFacturasImpuesto4RecargoEquivalenciaCuota);
                factura.Impuesto4RecEqCuota = Convert.ToDecimal(respuestaMetadatos.Items.Where(m => m.IdMetadata == idMetadatoArchivadorImpuesto4RecargoEquivalenciaCuota).FirstOrDefault().DecimalValue);

                factura.RutaDescarga = JsonConfig.URLVisorDocumentos;

                return factura;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Se produjo un error generando el objeto Factura - Error {0}", ex.Message);
                return null;
            }


        }

        private XmlDocument GenerarXMLFactura(ClienteSolpheo clienteSolpheo, Login loginSolpheo, Configuracion JsonConfig, Factura factura)
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

            string numFactAux = factura.NumeroFactura;
            if (numFactAux.Length > 20)
            {
                numFactAux = numFactAux.Substring(numFactAux.Length - 20, 20);
            }

            XmlText tipofact = doc.CreateTextNode(numFactAux);
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

            XmlText Cif;

            if (!string.IsNullOrEmpty(factura.CIFProveedor))
            {
                Cif = doc.CreateTextNode(factura.CIFProveedor);
            }
            else
            {
                Cif = doc.CreateTextNode(factura.RazonSocialProveedor.Split('#')[1].Trim());
            }


            CifEntidad.AppendChild(Cif);
            Entidad.AppendChild(CifEntidad);
            XmlElement Impuestos = doc.CreateElement(string.Empty, "Impuestos", string.Empty);
            fact.AppendChild(Impuestos);
            if (factura.Impuesto1Tipo != null)
            {
                XmlElement Impuesto1 = doc.CreateElement(string.Empty, "Impuesto", string.Empty);
                Impuestos.AppendChild(Impuesto1);
                XmlElement Base1 = doc.CreateElement(string.Empty, "Base", string.Empty);
                XmlText baseTexto1 = doc.CreateTextNode(factura.Impuesto1Base.ToString().Replace(",", "."));
                Base1.AppendChild(baseTexto1);
                Impuesto1.AppendChild(Base1);
                XmlElement Tipo1 = doc.CreateElement(string.Empty, "Tipo", string.Empty);
                XmlText tipoTexto1 = doc.CreateTextNode(factura.Impuesto1Tipo.Replace(",", "."));
                Tipo1.AppendChild(tipoTexto1);
                Impuesto1.AppendChild(Tipo1);
                XmlElement CuotaIVA1 = doc.CreateElement(string.Empty, "CuotaIVA", string.Empty);
                XmlText cuotaIVATexto1 = doc.CreateTextNode(factura.Impuesto1Cuota.ToString().Replace(",", "."));
                CuotaIVA1.AppendChild(cuotaIVATexto1);
                Impuesto1.AppendChild(CuotaIVA1);
                XmlElement RecEq1 = doc.CreateElement(string.Empty, "RecEq", string.Empty);
                XmlText recEqTexto1 = doc.CreateTextNode(factura.Impuesto1RecEqTipo == null ? "" : factura.Impuesto1RecEqTipo.Replace(",", "."));
                RecEq1.AppendChild(recEqTexto1);
                Impuesto1.AppendChild(RecEq1);
                XmlElement CuotaReq1 = doc.CreateElement(string.Empty, "CuotaReq", string.Empty);
                XmlText cuotaReqTexto1 = doc.CreateTextNode((factura.Impuesto1RecEqCuota == 0 ? "" : factura.Impuesto1RecEqCuota.ToString().Replace(",", ".")));
                CuotaReq1.AppendChild(cuotaReqTexto1);
                Impuesto1.AppendChild(CuotaReq1);
            }
            if (factura.Impuesto2Tipo != null)
            {
                XmlElement Impuesto2 = doc.CreateElement(string.Empty, "Impuesto", string.Empty);
                Impuestos.AppendChild(Impuesto2);
                XmlElement Base2 = doc.CreateElement(string.Empty, "Base", string.Empty);
                XmlText baseTexto2 = doc.CreateTextNode(factura.Impuesto2Base.ToString().Replace(",", "."));
                Base2.AppendChild(baseTexto2);
                Impuesto2.AppendChild(Base2);
                XmlElement Tipo2 = doc.CreateElement(string.Empty, "Tipo", string.Empty);
                XmlText tipoTexto2 = doc.CreateTextNode(factura.Impuesto2Tipo.Replace(",", "."));
                Tipo2.AppendChild(tipoTexto2);
                Impuesto2.AppendChild(Tipo2);
                XmlElement CuotaIVA2 = doc.CreateElement(string.Empty, "CuotaIVA", string.Empty);
                XmlText cuotaIVATexto2 = doc.CreateTextNode(factura.Impuesto2Cuota.ToString().Replace(",", "."));
                CuotaIVA2.AppendChild(cuotaIVATexto2);
                Impuesto2.AppendChild(CuotaIVA2);
                XmlElement RecEq2 = doc.CreateElement(string.Empty, "RecEq", string.Empty);
                XmlText recEqTexto2 = doc.CreateTextNode(factura.Impuesto2RecEqTipo == null ? "" : factura.Impuesto2RecEqTipo.Replace(",", "."));
                RecEq2.AppendChild(recEqTexto2);
                Impuesto2.AppendChild(RecEq2);
                XmlElement CuotaReq2 = doc.CreateElement(string.Empty, "CuotaReq", string.Empty);
                XmlText cuotaReqTexto2 = doc.CreateTextNode((factura.Impuesto2RecEqCuota == 0 ? "" : factura.Impuesto2RecEqCuota.ToString().Replace(",", ".")));
                CuotaReq2.AppendChild(cuotaReqTexto2);
                Impuesto2.AppendChild(CuotaReq2);
            }
            if (factura.Impuesto3Tipo != null)
            {
                XmlElement Impuesto3 = doc.CreateElement(string.Empty, "Impuesto", string.Empty);
                Impuestos.AppendChild(Impuesto3);
                XmlElement Base3 = doc.CreateElement(string.Empty, "Base", string.Empty);
                XmlText baseTexto3 = doc.CreateTextNode(factura.Impuesto3Base.ToString().Replace(",", "."));
                Base3.AppendChild(baseTexto3);
                Impuesto3.AppendChild(Base3);
                XmlElement Tipo3 = doc.CreateElement(string.Empty, "Tipo", string.Empty);
                XmlText tipoTexto3 = doc.CreateTextNode(factura.Impuesto3Tipo.Replace(",", "."));
                Tipo3.AppendChild(tipoTexto3);
                Impuesto3.AppendChild(Tipo3);
                XmlElement CuotaIVA3 = doc.CreateElement(string.Empty, "CuotaIVA", string.Empty);
                XmlText cuotaIVATexto3 = doc.CreateTextNode(factura.Impuesto3Cuota.ToString().Replace(",", "."));
                CuotaIVA3.AppendChild(cuotaIVATexto3);
                Impuesto3.AppendChild(CuotaIVA3);
                XmlElement RecEq3 = doc.CreateElement(string.Empty, "RecEq", string.Empty);
                XmlText recEqTexto3 = doc.CreateTextNode(factura.Impuesto3RecEqTipo == null ? "" : factura.Impuesto3RecEqTipo.Replace(",", "."));
                RecEq3.AppendChild(recEqTexto3);
                Impuesto3.AppendChild(RecEq3);
                XmlElement CuotaReq3 = doc.CreateElement(string.Empty, "CuotaReq", string.Empty);
                XmlText cuotaReqTexto3 = doc.CreateTextNode((factura.Impuesto3RecEqCuota == 0 ? "" : factura.Impuesto3RecEqCuota.ToString().Replace(",", ".")));
                CuotaReq3.AppendChild(cuotaReqTexto3);
                Impuesto3.AppendChild(CuotaReq3);
            }
            if (factura.Impuesto4Tipo != null)
            {
                XmlElement Impuesto4 = doc.CreateElement(string.Empty, "Impuesto", string.Empty);
                Impuestos.AppendChild(Impuesto4);
                XmlElement Base4 = doc.CreateElement(string.Empty, "Base", string.Empty);
                XmlText baseTexto4 = doc.CreateTextNode(factura.Impuesto4Base.ToString().Replace(",", "."));
                Base4.AppendChild(baseTexto4);
                Impuesto4.AppendChild(Base4);
                XmlElement Tipo4 = doc.CreateElement(string.Empty, "Tipo", string.Empty);
                XmlText tipoTexto4 = doc.CreateTextNode(factura.Impuesto4Tipo.Replace(",", "."));
                Tipo4.AppendChild(tipoTexto4);
                Impuesto4.AppendChild(Tipo4);
                XmlElement CuotaIVA4 = doc.CreateElement(string.Empty, "CuotaIVA", string.Empty);
                XmlText cuotaIVATexto4 = doc.CreateTextNode(factura.Impuesto4Cuota.ToString().Replace(",", "."));
                CuotaIVA4.AppendChild(cuotaIVATexto4);
                Impuesto4.AppendChild(CuotaIVA4);
                XmlElement RecEq4 = doc.CreateElement(string.Empty, "RecEq", string.Empty);
                XmlText recEqTexto4 = doc.CreateTextNode(factura.Impuesto4RecEqTipo == null ? "" : factura.Impuesto4RecEqTipo.Replace(",", "."));
                RecEq4.AppendChild(recEqTexto4);
                Impuesto4.AppendChild(RecEq4);
                XmlElement CuotaReq4 = doc.CreateElement(string.Empty, "CuotaReq", string.Empty);
                XmlText cuotaReqTexto4 = doc.CreateTextNode((factura.Impuesto4RecEqCuota == 0 ? "" : factura.Impuesto4RecEqCuota.ToString().Replace(",", ".")));
                CuotaReq4.AppendChild(cuotaReqTexto4);
                Impuesto4.AppendChild(CuotaReq4);
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
            bool encontradosNuevosFicheros = false;

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
                        encontradosNuevosFicheros = true;

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

                if (encontradosNuevosFicheros)
                {
                    await ActualizarListaSolpheoCodigosObras(JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);
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


        public async Task<bool> EnviarCorreosBienvenidaInicialesPendientes(string url, string ApiKey)
        {
            bool resultadoOK = true;

            log.Information($"EnviarCorreosBienvenidaInicialesPendientes - Inicio Metodo");


            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("ApiKey", ApiKey);

                    var responseClient = await client.PostAsync(new Uri(url + $"/ePortalProveedores/EnviarCorreosBienvenidaInicialesPendientes"), null);
                    if (!responseClient.IsSuccessStatusCode)
                    {
                        log.Information($"EnviarCorreosBienvenidaInicialesPendientes - Error en llamada al API del portal de proveedores para enviar los correos de bienvenida iniciales");
                        resultadoOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"EnviarCorreosBienvenidaInicialesPendientes - Error en llamada al API del portal de proveedores para enviar los correos de bienvenida iniciales - {ex.Message}");
                resultadoOK = false;
            }

            return resultadoOK;
        }

        public async 
        Task
ProcesarFacturaAEnviarAERP(string idFileItemFactura, ClienteSolpheo clienteSolpheo, Login loginSolpheo, Configuracion JsonConfig)
        {
            try
            {
                string RutaAccesoXMLEntradaERP = JsonConfig.XMLRutaEntradaERP;

                log.Information($"GetFacturasSolpheo - Procesando fileItem {idFileItemFactura} para generar su XML");

                var respuestaMetadatos = await clienteSolpheo.MetadatasFileItemAsync(
                    loginSolpheo.AccessToken,
                    int.Parse(JsonConfig.IdFileContainerArchivadorFacturas),
                    int.Parse(idFileItemFactura));

                Factura factura = GenerarObjetoFactura(jsonConfig, respuestaMetadatos);
                factura.Identificador = idFileItemFactura;

                if (factura != null)
                {
                    if (File.Exists(RutaAccesoXMLEntradaERP + "\\" + factura.Identificador + ".xml") || File.Exists(RutaAccesoXMLEntradaERP + "\\Procesadas\\" + factura.Identificador + ".xml"))
                    {
                        log.Information($"GetFacturasSolpheo - El XML fileItem {factura.Identificador} ya ha sido generado previamente existiendo en la carpeta de entrada al ERP o en la subcarpeta Procesadas");
                        return;
                    }

                    XmlDocument doc = GenerarXMLFactura(clienteSolpheo, loginSolpheo, JsonConfig, factura);

                    //Se avanza el WF al estado "Pendiente Respuesta Contabilización ERP"
                    var resultIdWF = await clienteSolpheo.GetIdWorkFlowAsync(loginSolpheo.AccessToken, int.Parse(factura.Identificador));
                    if (!resultIdWF.Resultado)
                    {
                        log.Information($"GetFacturasSolpheo - No se ha obtenido Id Workflow para fileItem {factura.Identificador} por lo que no se copia el XML al directorio entrada del ERP.");
                        return;
                    }

                    var avance = await clienteSolpheo.AvanzarWorkFlowAsync(
                        loginSolpheo.AccessToken,
                        int.Parse(factura.Identificador),
                        JsonConfig.IdSalidaWorkFlowTareaPendienteEnvioAERP,
                        int.Parse(resultIdWF.Id), true);

                    if (!avance.Resultado)
                    {
                        log.Error("GetFacturasSolpheo - Error al avanzar Workflow tras enviar documento a ERP con IdFileItem " + factura.Identificador);
                        return;
                    }

                    doc.Save(RutaAccesoXMLEntradaERP + "\\" + factura.Identificador + ".xml");
                    log.Information($"GetFacturasSolpheo - XML copiado a directorio entrada del ERP par fileItem {factura.Identificador}");
                }




            }
            catch (Exception ex)
            {
                log.Error("GetFacturasSolpheo - Error Generando XML - " + ex.Message, ex);
            }

            return;
        }


        public async Task<bool> ActualizarListaSolpheoCodigosObras(string url, string ApiKey)
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

                    var responseClient = await client.PostAsync(new Uri(url + $"/ePortalProveedores/ActualizarListaValoresSolpheoCodigosObra"), null);
                    if (!responseClient.IsSuccessStatusCode)
                    {
                        log.Information($"actualizarListaSolpheoCodigosObras - Error en llamada al API del portal de proveedores ActualizarListaValoresSolpheoCodigosObra");
                        resultadoOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"actualizarListaSolpheoCodigosObras - Error en llamada al API del portal de proveedores ActualizarListaValoresSolpheoCodigosObra - {ex.Message}");
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
            bool encontradosNuevosFicheros = false;

            if (!Directory.Exists(JsonConfig.CSVProveedoresRutaERP))
            {
                log.Information($"FicheroCSV_Proveedores - No existe el directorio de salida de este CSV -> {JsonConfig.CSVProveedoresRutaERP}");
                return false;
            }

            string[] allfiles = Directory.GetFiles(JsonConfig.CSVProveedoresRutaERP, "*.CSV", SearchOption.TopDirectoryOnly);
            foreach (var file in allfiles)
            {
                encontradosNuevosFicheros = true;

                try
                {
                    FileInfo info = new FileInfo(file);

                    log.Information($"FicheroCSV_Proveedores - Procesando fichero {info.Name}");

                    bool resultadoFicheroOK = true;

                    var lineCount = 0;
                    string line = string.Empty;
                    using (var readerlines = File.OpenText(file))
                    {
                        while ((line = readerlines.ReadLine()) != null)
                        {
                            if (!line.Equals(string.Empty))
                            {
                                lineCount++;
                            }
                        }
                    }

                    string CSVCon1SoloProveedor = (lineCount == 1 ? "1" : "0");

                    if (CSVCon1SoloProveedor == "1")
                    {
                        log.Information($"FicheroCSV_Proveedores - Procesando fichero {info.Name} - Fichero con 1 sola línea");
                    }


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

                                        foreach (string email in emails)
                                        {
                                            if (!string.IsNullOrEmpty(email))
                                            {
                                                var resultadoEmail = await GrabaProveedor(datosProveedor[0], datosProveedor[1], email, datosProveedor[3], CSVCon1SoloProveedor, JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);

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

            if (encontradosNuevosFicheros)
            {
                await ActualizarListasSolpheoCIFYRazonesSocialesProveedores(JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);
            }

            await EnviarCorreosBienvenidaInicialesPendientes(JsonConfig.URLAPIPortalProveedores, JsonConfig.ApiKeyAPIPortalProveedores);


            return true;
        }

        public async Task<bool> GrabaProveedor(string CIF, string Nombre, string Email, string fechaReenvioEmailBienvenida, string forzarEnvioCorreoBienvenida, string url, string ApiKey)
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

                    var parameters = new Dictionary<string, string>();

                    parameters.Add("Nombre", Nombre + " # " + CIF);
                    parameters.Add("NIF", CIF);
                    parameters.Add("Email", Email);
                    parameters.Add("CodigoPerfil", "PROV");
                    parameters.Add("FechaMod", fechaReenvioEmailBienvenida);
                    parameters.Add("ForzarEnvioCorreoBienvenida", forzarEnvioCorreoBienvenida);

                    string output = JsonConvert.SerializeObject(parameters);
                    var jsonData = new StringContent(output, Encoding.UTF8, "application/json");

                    var responseClient = await client.PostAsync(url + "/ePortalProveedores/AltaProveedores", jsonData);


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

        public async Task<bool> ActualizarListasSolpheoCIFYRazonesSocialesProveedores(string url, string ApiKey)
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

                    var responseClient = await client.PostAsync(new Uri(url + $"/ePortalProveedores/ActualizarListaValoresSolpheoProveedores"), null);
                    if (!responseClient.IsSuccessStatusCode)
                    {
                        log.Information($"ActualizarListasSolpheoCIFYRazonesSocialesProveedores - Error en llamada al API del portal de proveedores ActualizarListaValoresSolpheoProveedores");
                        resultadoOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"ActualizarListasSolpheoCIFYRazonesSocialesProveedores - Error en llamada al API del portal de proveedores ActualizarListaValoresSolpheoProveedores - {ex.Message}");
                resultadoOK = false;
            }

            return resultadoOK;
        }


        public async Task<bool> ActualizarRegistrosSolpheoProveedores(string url, string ApiKey)
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

                    var responseClient = await client.PostAsync(new Uri(url + $"/ePortalProveedores/ActualizarRegistroSolpheoProveedores"), null);
                    if (!responseClient.IsSuccessStatusCode)
                    {
                        log.Information($"ActualizarRegistrosSolpheoProveedores - Error en llamada al API del portal de proveedores");
                        resultadoOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"ActualizarRegistrosSolpheoProveedores - Error en llamada al API del portal de proveedores - {ex.Message}");
                resultadoOK = false;
            }

            return resultadoOK;
        }


        public async Task<bool> ActualizarRegistrosSolpheoCodigosObra(string url, string ApiKey)
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

                    var responseClient = await client.PostAsync(new Uri(url + $"/ePortalProveedores/ActualizarRegistroSolpheoCodigosObra"), null);
                    if (!responseClient.IsSuccessStatusCode)
                    {
                        log.Information($"ActualizarRegistrosSolpheoCodigosObra - Error en llamada al API del portal de proveedores");
                        resultadoOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"ActualizarRegistrosSolpheoCodigosObra - Error en llamada al API del portal de proveedores - {ex.Message}");
                resultadoOK = false;
            }

            return resultadoOK;
        }
    }
}
