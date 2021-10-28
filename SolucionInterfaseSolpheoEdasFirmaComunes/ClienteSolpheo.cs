using Kyocera.Solpheo.ApiClient;
using Kyocera.Solpheo.ApiClient.Models;
using Kyocera.Solpheo.ApiClient.Utils;
using Newtonsoft.Json;
using SolucionFacturasComunes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;


namespace SolucionFacturasComunes
{
    public class ClienteSolpheo : IAPISolpheo
    {
        private string _urlTenant = string.Empty;

        /// <summary>
        /// Initialize a new instance of the <see cref="Client"/> class
        /// </summary>
        /// <param name="urlTenant">Url tenant (http(s)://tenant.dominio.com)</param>
        public ClienteSolpheo(string urlTenant)
        {
            this._urlTenant = urlTenant + "/";
        }

        /// <summary>
        /// Method to get access token, login Solpheo Web
        /// </summary>
        /// <param name="user">User login</param>
        /// <param name="pass">Password login</param>
        /// <param name="tenant">Tenant</param>
        /// <param name="type">Type client id (roclient)</param>
        /// <param name="secret">Secret client (secret)</param>
        /// <returns>Login information</returns>
        public async Task<Login> LoginAsync(string user, string pass, string tenant, string client_id, string client_secret, string scope)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                var parameters = new Dictionary<string, string>();

                parameters.Add("username", user);
                parameters.Add("password", pass);
                parameters.Add("grant_type", "password");
                parameters.Add("client_id", client_id);
                parameters.Add("scope", scope);
                parameters.Add("client_secret", client_secret);
                parameters.Add("acr_values", "tenant:" + tenant);

                var responseClient = await client.PostAsync(Constants.URLTOKEN, new FormUrlEncodedContent(parameters));
                string data = await responseClient.Content.ReadAsStringAsync();
                if (responseClient.IsSuccessStatusCode)
                {
                    var login = JsonConvert.DeserializeObject<Login>(data);
                    return login;
                }
                else
                {
                    LoginError loginError = JsonConvert.DeserializeObject<LoginError>(data);
                    throw new Exception(loginError.ErrorDescription);
                }
            }
        }

        /// <summary>
        /// Revocation token user
        /// </summary>
        /// <param name="token">Token to revocate</param>
        /// <returns>True if revocation success, false otherwise</returns>
        public async Task<bool> LogoutAsync(string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Util.EncodeBase64("roclient:secret"));
                var parameters = new Dictionary<string, string>();

                parameters.Add("token", token);
                parameters.Add("token_type_hint", "access_token");

                var responseClient = await client.PostAsync(Constants.URLLOGOUT, new FormUrlEncodedContent(parameters));

                if (responseClient.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }
        }

        //Fileitem de un Registro o Archivador con filtro avanzado

        public async Task<PagedList<FileContainerListViewModel>> FileItemsAdvancednested(string token, int idFilecontainer, string jsonFiltro)
        {
            PagedList<FileContainerListViewModel> fileitems = null;
            PagedList<FileContainerListViewModel> files = new PagedList<FileContainerListViewModel>();
            var it = new List<FileContainerListViewModel>();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


                var jsonData = new StringContent(jsonFiltro, Encoding.UTF8, "application/json");
                int pageNumber = 0;

                //var a = await client.GetAsync(this._urlTenant + "api/fileitems/351");
                var responseClient = await client.PostAsync($"{this._urlTenant}{"api/fileitems/search/advancednested/"}{idFilecontainer}" + "/?filtro.pageSize=50", jsonData);


                if (responseClient.IsSuccessStatusCode)
                {
                    string data = await responseClient.Content.ReadAsStringAsync();

                    try
                    {
                        fileitems = JsonConvert.DeserializeObject<PagedList<FileContainerListViewModel>>(data);
                        it.AddRange(fileitems.Items);

                        while (it.Count() < fileitems.TotalCount)
                        {
                            pageNumber++;
                            jsonData = new StringContent(jsonFiltro, Encoding.UTF8, "application/json");

                            responseClient = await client.PostAsync($"{this._urlTenant + "/"}{"api/fileitems/search/advancednested/"}{idFilecontainer}" + "/?filtro.state=1&filtro.pageSize=50&filtro.pageIndex=" + pageNumber, jsonData);
                            if (responseClient.IsSuccessStatusCode)
                            {
                                data = await responseClient.Content.ReadAsStringAsync();

                                try
                                {
                                    fileitems = JsonConvert.DeserializeObject<PagedList<FileContainerListViewModel>>(data);
                                    it.AddRange(fileitems.Items);

                                }
                                catch (Exception)
                                {
                                    throw new Exception();
                                }

                            }
                        }
                    }
                    catch (Exception)
                    {
                        throw new Exception();
                    }

                    files.Items = it;
                    return files;
                }

                return null;
            }
        }

        public async Task<FileContainerListViewModel> FileItemsByIdAsync(string token, int idFilecontainer, int idFileItem)
        {
            FileContainerListViewModel fileitem = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);



                var responseClient = await client.GetAsync($"{Constants.URLFILEITEMS}/{idFilecontainer}/{idFileItem}");
                if (responseClient.IsSuccessStatusCode)
                {
                    string data = await responseClient.Content.ReadAsStringAsync();
                    fileitem = JsonConvert.DeserializeObject<FileContainerListViewModel>(data);
                }
            }



            return fileitem;
        }

        public async Task<PagedList<FileContainerListViewModel>> BuscarPorIfFileItem(string token, int idFilecontainer, int idFileItem)
        {
            PagedList<FileContainerListViewModel> fileitems = null;
            PagedList<FileContainerListViewModel> files = new PagedList<FileContainerListViewModel>();
            var it = new List<FileContainerListViewModel>();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{this._urlTenant}{"api/fileitems/"}{idFilecontainer}" + "/" + idFileItem);

                if (responseClient.IsSuccessStatusCode)
                {
                    string data = await responseClient.Content.ReadAsStringAsync();

                    try
                    {
                        fileitems = JsonConvert.DeserializeObject<PagedList<FileContainerListViewModel>>(data);
                        it.AddRange(fileitems.Items);
                    }
                    catch (Exception)
                    {
                        throw new Exception();
                    }
                    files.Items = it;
                    return files;
                }
                return null;
            }
        }

        // Metadatos de un IdFileItem
        public async Task<FileDownload> DownloadFileItemsAsync(string token, int idFileContainer, List<FileContainerListViewModel> FileItems)
        {
            FileDownload fileInformation = new FileDownload();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + " / ");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new FileItems();
                content.items = FileItems;

                var myJson = JsonConvert.SerializeObject(content);

                var responseClient = await client.PostAsync($"{this._urlTenant + "/"}" + "api/download/fileitems/" + idFileContainer, new StringContent(myJson, Encoding.UTF8, "application/json"));

                if (responseClient.IsSuccessStatusCode)
                {
                    Stream data = await responseClient.Content.ReadAsStreamAsync();
                    MemoryStream ms = new MemoryStream();
                    data.CopyTo(ms);
                    byte[] file = ms.ToArray();
                    fileInformation.bytearray = file;
                }

            }
            return fileInformation;

        }


        #region Workplace

        /// <summary>
        /// Get boxes workplace
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <param name="personal">Indicate if we only want the personal drive </param>
        /// <returns>List boxes paged</returns>
        public async Task<PagedList<Boxes>> BoxesAsync(string token, int page, int pageSize, bool personal = false)
        {
            PagedList<Boxes> boxes = new PagedList<Boxes>();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"{Constants.URLBOXES}/{page}/{pageSize}?personal={personal.ToString()}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    boxes = JsonConvert.DeserializeObject<PagedList<Boxes>>(data);
                }

                return boxes;
            }
        }

        /// <summary>
        /// Create a new folder
        /// </summary>
        /// <param name="token"></param>
        /// <param name="idbox"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="idFolderParent"></param>
        /// <returns>Identifier new folder</returns>
        public async Task<ResultId> CreateFolderAsync(string token, int idbox, string name, string description, int idFolderParent)
        {
            ResultId result = new ResultId();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new CreateFolderViewModel()
                {
                    Description = description,
                    IdBox = idbox,
                    Name = name,
                    IdFolderPadre = idFolderParent
                };


                var response = await client.PostAsJsonAsync<CreateFolderViewModel>($"{Constants.URLCREATEFOLDER}/{idbox}", content);

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<ResultId>(data);
                }

                return result;
            }
        }

        /// <summary>
        /// Get folders and documents of a box
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idbox">Identifier box</param>
        /// <param name="idfolder">Identifier folder</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <returns>List items paged</returns>
        public async Task<PagedList<ItemBox>> FoldersAsync(string token, int idbox, int idfolder, int page, int pageSize)
        {
            PagedList<ItemBox> itemBoxes = new PagedList<ItemBox>();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"{Constants.URLLISTITEMBOX}/{idbox}?filter.id={idfolder}&filter.pageIndex={page}&filter.pageSize={pageSize}&filter.sortColumn=createdDate&filter.sortOrder=desc");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    itemBoxes = JsonConvert.DeserializeObject<PagedList<ItemBox>>(data);
                }

                return itemBoxes;
            }
        }

        /// <summary>
        /// Upload a new document in a box
        /// </summary>
        /// <param name="idbox">Identifier box</param>
        /// <param name="token">Access token</param>
        /// <param name="namefile">Name file</param>
        /// <param name="file">File in bytes</param>
        /// <param name="idFolder">Identifier folder</param>
        /// <returns>Identifier new document</returns>
        public async Task<ResultId> UploadDocumentAsync(int idbox, string token, string namefile, byte[] file, int idFolder)
        {
            ResultId IdDocument = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StreamContent(new MemoryStream(file)), "file", namefile);
                    content.Add(
                        new StringContent(idFolder.ToString()),
                        "idFolder",
                        idFolder.ToString());
                    var responseClient = await client.PostAsync($"{Constants.URLUPLOADFILEWORKPLACE}/{idbox}", content);

                    if (responseClient.IsSuccessStatusCode)
                    {
                        string data = await responseClient.Content.ReadAsStringAsync();
                        IdDocument = JsonConvert.DeserializeObject<ResultId>(data);
                    }
                }
            }

            return IdDocument;
        }

        /// <summary>
        /// Download a folder
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="id">Identifier file</param>
        /// <param name="name">Name file download</param>
        /// <param name="ext">Extension file, default zip</param>
        /// <returns>Information file downloaded</returns>
        public async Task<FileDownload> DownloadDocumentsync(string token, int id, string name, string ext)
        {
            FileDownload fileInformation = new FileDownload();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{Constants.DOWNLOADDOCUMENT}/{id}");

                if (responseClient.IsSuccessStatusCode)
                {
                    Stream data = await responseClient.Content.ReadAsStreamAsync();
                    MemoryStream ms = new MemoryStream();
                    data.CopyTo(ms);
                    byte[] file = ms.ToArray();
                    fileInformation.bytearray = file;
                    fileInformation.NameFichero = name;
                    fileInformation.Ext = ext;
                }
            }
            return fileInformation;
        }

        /// <summary>
        /// Download a folder
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="id">Identifier folder</param>
        /// <param name="name">Name file download</param>
        /// <param name="ext">Extension file, default zip</param>
        /// <returns>Information file downloaded</returns>
        public async Task<FileDownload> DownloadFolderAsync(string token, int id, string name, string ext)
        {
            FileDownload fileInformation = new FileDownload();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{Constants.DOWNLOADFOLDER}/{id}");

                if (responseClient.IsSuccessStatusCode)
                {
                    Stream data = await responseClient.Content.ReadAsStreamAsync();
                    MemoryStream ms = new MemoryStream();
                    data.CopyTo(ms);
                    byte[] file = ms.ToArray();
                    fileInformation.bytearray = file;
                    fileInformation.NameFichero = name;
                    fileInformation.Ext = ext;
                }
            }
            return fileInformation;
        }

        #endregion

        #region Filecontainers

        /// <summary>
        /// Get tree level
        /// </summary>
        /// <param name="token">Access token</param>
        /// <returns>List levels</returns>
        public async Task<IEnumerable<LevelItemViewModel>> Levels(string token)
        {
            IEnumerable<LevelItemViewModel> levelsTree = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{Constants.URLLEVELTREE}");
                if (responseClient.IsSuccessStatusCode)
                {
                    string data = await responseClient.Content.ReadAsStringAsync();
                    levelsTree = JsonConvert.DeserializeObject<IEnumerable<LevelItemViewModel>>(data);
                }
            }

            return levelsTree;
        }

        /// <summary>
        /// Get file all file containers
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <returns>File containers paged</returns>
        public async Task<PagedList<FileContainerListViewModel>> FileContainersAsync(string token, int page, int pageSize)
        {
            PagedList<FileContainerListViewModel> filecontainers = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{Constants.URLFILECONTAINERS}/{page}/{pageSize}");
                if (responseClient.IsSuccessStatusCode)
                {
                    string data = await responseClient.Content.ReadAsStringAsync();
                    filecontainers = JsonConvert.DeserializeObject<PagedList<FileContainerListViewModel>>(data);
                }
            }

            return filecontainers;
        }

        class Person
        {
            public string Name { get; set; }
            public string Occupation { get; set; }

            public override string ToString()
            {
                return $"{Name}: {Occupation}";
            }
        }


        /// <summary>
        /// Get file items (documents) file container
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFilecontainer">Identifier file container</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <returns>Items paged</returns>
        public async Task<PagedList<FileContainerListViewModel>> FileItemsAsync(string token, int idFilecontainer, int page, int pageSize)
        {

            return null;
        }

        /// <summary>
        /// Get file items (documents) file container byvalue
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFilecontainer">Identifier file container</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <returns>Items paged</returns>
        public async Task<List<FileContainerListViewModel>> FileItemsByValueAsync(string token, int idFilecontainer, int idMetadata, string metadataValue, bool matchWholeWord)
        {
            PagedList<FileContainerListViewModel> fileitems = null;
            List<FileContainerListViewModel> file_item = null;
            int page = 0;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{Constants.URLFILEITEMS}/byvalue/{idFilecontainer}/{idMetadata}/{metadataValue}/{matchWholeWord}/?filter.pageIndex={page}");
                if (responseClient.IsSuccessStatusCode)
                {
                    string data = await responseClient.Content.ReadAsStringAsync();
                    fileitems = JsonConvert.DeserializeObject<PagedList<FileContainerListViewModel>>(data);

                    file_item = fileitems.Items.ToList();

                    foreach (var item in fileitems.Items)
                    {
                        if (item.State == 3) file_item.Remove(item);
                    }

                    for (var e = page; e <= fileitems.TotalCount / 10; e++)
                    {
                        page = e + 1;
                        responseClient = await client.GetAsync($"{Constants.URLFILEITEMS}/byvalue/{idFilecontainer}/{idMetadata}/{metadataValue}/{matchWholeWord}/?filter.pageIndex={page}");
                        if (responseClient.IsSuccessStatusCode)
                        {
                            data = await responseClient.Content.ReadAsStringAsync();
                            fileitems = JsonConvert.DeserializeObject<PagedList<FileContainerListViewModel>>(data);
                            {
                                foreach (FileContainerListViewModel fileitem in fileitems.Items)
                                {
                                    if (fileitem.State != 3)
                                    {
                                        file_item.Add(fileitem);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return file_item;
        }

        /// <summary>
        /// Get metadatas of a container
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFileContainer">Identifier file container</param>
        /// <returns>Metadatas paged</returns>
        public async Task<PagedList<MetadataFileItemValue>> MetadatasFileContainerAsync(string token, int idFileContainer)
        {
            PagedList<MetadataFileItemValue> metadatasFileContainer = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{Constants.URLMETADATASFILEITEM}/{idFileContainer}");
                if (responseClient.IsSuccessStatusCode)
                {
                    string data = await responseClient.Content.ReadAsStringAsync();
                    metadatasFileContainer = JsonConvert.DeserializeObject<PagedList<MetadataFileItemValue>>(data);
                }
            }

            return metadatasFileContainer;
        }

        /// <summary>
        /// Get metadatas of a file item
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFileContainer">Identifier file container</param>
        /// <param name="idFileItem">Identifier file item</param>
        /// <returns>Metadatas file item paged</returns>
        public async Task<PagedList<MetadataFileItemValue>> MetadatasFileItemAsync(string token, int idFileContainer, int idFileItem)
        {
            PagedList<MetadataFileItemValue> metadatasFileItem = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{Constants.URLMETADATASFILEITEM}/{idFileContainer}?idFileItem={idFileItem}&pageSize=999&withOutValues=false");
                if (responseClient.IsSuccessStatusCode)
                {
                    string data = await responseClient.Content.ReadAsStringAsync();
                    metadatasFileItem = JsonConvert.DeserializeObject<PagedList<MetadataFileItemValue>>(data);
                }
            }

            return metadatasFileItem;
        }

        /// <summary>
        /// Update metadatas file item
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFileContainer">Identifier file container</param>
        /// <param name="metadatas">Metadtas to update</param>
        /// <returns>Task empty</returns>
        public async Task<ResultadoSolpheo> UpdateMetadatasFileItemAsync(string token, int idFileContainer, FileContainerMetadataValue[] metadatas)
        {
            ResultadoSolpheo SolpheoResult = new ResultadoSolpheo();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.PutAsJsonAsync<FileContainerMetadataValue[]>($"{Constants.URLMETADATASFILEITEM}/{idFileContainer}", metadatas);

                string data = await responseClient.Content.ReadAsStringAsync();

                if (responseClient.IsSuccessStatusCode)
                {
                    SolpheoResult.Resultado = true;
                    SolpheoResult.Mensaje = String.Empty;
                }
                else
                {
                    SolpheoResult.Resultado = false;
                    SolpheoResult.Mensaje = data;
                }
            }
            return SolpheoResult;
        }
        public async Task<ResultadoSolpheo> UpdateVariablesWorkFlowAsync(string token, int idFileItem, int idMetadata, string variableName, string variableType, string variableValue, int idWorkflowActivity)
        {
            ResultadoSolpheo SolpheoResult = new ResultadoSolpheo();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var valorTipo = 0;
                var tipo = String.Empty;

                switch (variableType)
                {
                    case "StringValue":
                        valorTipo = 1;
                        tipo = "stringValue";
                        break;
                    case "IntValue":
                        valorTipo = 2;
                        tipo = "intValue";
                        break;
                    case "DecimalValue":
                        valorTipo = 3;
                        tipo = "decimalValue";
                        break;
                    case "DateTimeValue":
                        valorTipo = 4;
                        tipo = "dateTimeValue";
                        break;
                    case "ValueBit":
                        valorTipo = 5;
                        tipo = "valueBit";
                        break;
                }

                string json = "{\'relationsMetadatas\':[{\'metadata\':{\'idMetadata\':" + idMetadata +
                            ",\'idFileItem\':" + idFileItem + ",\'name\':\'" + variableName + "\',\'idType\': " + valorTipo + ",\'" + tipo + "\':\'" + variableValue +
                            "\'},\'name\':\'" + variableName + "\',\'idMetadataFileContainer\':" + idMetadata +
                            ",\'value\':\'" + variableValue + "\',\'type\':" + valorTipo + "}]}";

                var responseClient = await client.PutAsync($"{this._urlTenant + "/"}" + "api/workflowactivity/" + idWorkflowActivity + "/relationsMetadatas", new StringContent(json, Encoding.UTF8, "application/json"));
                string data = await responseClient.Content.ReadAsStringAsync();

                if (responseClient.IsSuccessStatusCode)
                {
                    SolpheoResult.Resultado = true;
                    SolpheoResult.Mensaje = String.Empty;
                }
                else
                {
                    SolpheoResult.Resultado = false;
                    SolpheoResult.Mensaje = data;
                }
            }
            return SolpheoResult;
        }

        /// <summary>
        /// Upload file item to file container
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFileContainer">Identifier file container</param>
        /// <param name="namefichero">Name file</param>
        /// <param name="file">File in bytes</param>
        /// <param name="metadatas">List metadatas with values</param>
        /// <param name="isRecord">Indicate if file is record or not</param>
        /// <returns>Identifier new file item</returns>
        public async Task<ResultId> UploadFileItemAsync(string token, int idFileContainer, string nameFile, byte[] file, FileContainerMetadataValue[] metadatas, bool isRecord)
        {
            ResultId IdDocument = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using (var content = new MultipartFormDataContent("---------------------------8070139587774"))
                {
                    if (!isRecord)
                    {
                        content.Add(new StreamContent(new MemoryStream(file)), "file", nameFile);
                    }

                    var json = JsonConvert.SerializeObject(metadatas);
                    content.Add(new StringContent(json), "metadatas");

                    var responseClient = await client.PostAsync($"{Constants.URLUPLOADFILECONTAINER}/{idFileContainer}/{isRecord}", content);
                    if (responseClient.IsSuccessStatusCode)
                    {
                        string data = await responseClient.Content.ReadAsStringAsync();
                        IdDocument = JsonConvert.DeserializeObject<ResultId>(data);
                    }
                }
            }

            return IdDocument;
        }

        public async Task<ResultId> ActualizarMetadato(string token, int idFileContainer, int idFileItem, int idMetadata, string variableName, string variableType, string variableValue)
        {
            ResultId metadatasFileContainer = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var valorTipo = 0;
                var tipo = String.Empty;

                switch (variableType)
                {
                    case "StringValue":
                        valorTipo = 1;
                        tipo = "stringValue";
                        break;
                    case "IntValue":
                        valorTipo = 2;
                        tipo = "intValue";
                        break;
                    case "DecimalValue":
                        valorTipo = 3;
                        tipo = "decimalValue";
                        break;
                    case "DateTimeValue":
                        valorTipo = 4;
                        tipo = "dateTimeValue";
                        break;
                    case "ValueBit":
                        valorTipo = 5;
                        tipo = "valueBit";
                        break;
                }

                string json = "{\'idMetadata\':" + idMetadata +
                           ",\'idFileItem\':" + idFileItem + ",\'name\':\'" + variableName + "\',\'idType\': " + valorTipo + ",\'" + tipo + "\':\'" + variableValue +
                           "\',\'name\':\'" + variableName + "\',\'idMetadataFileContainer\':" + idMetadata +
                           ",\'value\':\'" + variableValue + "\',\'type\':" + valorTipo + "}";

                var responseClient = await client.PutAsync($"{this._urlTenant + "/"}" + "api/filecontainersmetadatasvalues/" + idFileContainer + "/" + idFileItem + "/" + idMetadata, new StringContent(json, Encoding.UTF8, "application/json"));
                string data = await responseClient.Content.ReadAsStringAsync();

                if (responseClient.IsSuccessStatusCode)
                {
                    metadatasFileContainer = JsonConvert.DeserializeObject<ResultId>(data);
                }
            }

            return metadatasFileContainer;
        }

        public async Task<ResultadoSolpheo> GetIdWorkFlowAsync(string token, int idFileItem)
        {
            ResultadoSolpheo SolpheoResult = new ResultadoSolpheo();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var responseClient = await client.GetAsync($"{this._urlTenant + "/"}" + "api/workflowactivity/" + idFileItem);
                string data = await responseClient.Content.ReadAsStringAsync();

                if (responseClient.IsSuccessStatusCode && data != "null")
                {

                    var IdActivity = JsonConvert.DeserializeObject<ResultId>(data);
                    SolpheoResult.Mensaje = IdActivity.Id.ToString();
                    SolpheoResult.Resultado = true;
                }
                else
                {
                    SolpheoResult.Mensaje = data;
                    SolpheoResult.Resultado = false;
                }

            }
            return SolpheoResult;
        }

        public async Task<ResultadoSolpheo> ReemplazarDocumento(string token, int idFileItem, byte[] file, string nameFile)
        {
            ResultadoSolpheo SolpheoResult = new ResultadoSolpheo();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using (var content = new MultipartFormDataContent("----WebKitFormBoundary6BZNEYMSjcR5iiZw"))
                {
                    content.Add(new StreamContent(new MemoryStream(file)), "file", nameFile);
                    var responseClient = await client.PostAsync($"{this._urlTenant}{"api/fileitems/replace/workflow/"}{idFileItem}", content);

                    string data = await responseClient.Content.ReadAsStringAsync();

                    if (responseClient.IsSuccessStatusCode)
                    {
                        SolpheoResult.Resultado = true;
                        SolpheoResult.Mensaje = String.Empty;
                    }
                    else
                    {
                        SolpheoResult.Resultado = false;
                        SolpheoResult.Mensaje = data;
                    }
                }
            }
            return SolpheoResult;
        }



        public async Task<ResultadoSolpheo> AvanzarWorkFlowAsync(string token, int idFileItem, string idKey, int idWorkflowActivity)
        {
            ResultadoSolpheo SolpheoResult = new ResultadoSolpheo();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string json = "{\"idWorkflowActivity\":" + idWorkflowActivity + ",\"resultKey\":" + idKey + ",\"notCheckRequiredVariables\":false,\"replaceFile\":false}";

                var responseClient = await client.PostAsync($"{this._urlTenant + "/"}" + "api/workflowactivity/", new StringContent(json, Encoding.UTF8, "application/json"));

                string data = await responseClient.Content.ReadAsStringAsync();

                if (responseClient.IsSuccessStatusCode)
                {
                    SolpheoResult.Resultado = true;
                    SolpheoResult.Mensaje = String.Empty;
                }
                else
                {
                    SolpheoResult.Resultado = false;
                    SolpheoResult.Mensaje = data;
                }
            }
            return SolpheoResult;
        }

        #endregion

        #region Viewers

        /// <summary>
        /// Get url viewer
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idContainer">Identifier container</param>
        /// <param name="idFile">Identifier file</param>
        /// <param name="isFileContainer">Indicate if container is file container</param>
        /// <param name="extension">Extension file</param>
        /// <returns>Url viewer</returns>

        public async Task<string> Viewer(string token, int idContainer, int idFile, bool isFileContainer, string extension)
        {
            throw new Exception("Viewer not available");

        }
        /// <summary>
        /// Ger ulr viewer office online
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idContainer">Identifier container (box or file container)</param>
        /// <param name="idFile">Idnetifier file</param>
        /// <param name="isFileContainer">Indicate file is in file container</param>
        /// <returns>Url office online</returns>
        private async Task<string> ViewerOffice(string token, int idContainer, int idFile, bool isFileContainer)
        {

            throw new Exception("Not open with office");

        }

        /// <summary>
        /// Get url viewer office document
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idContainer">Identifier container (box or file container)</param>
        /// <param name="idFile">Idnetifier file</param>
        /// <param name="linkOffice">Endpoint link office</param>
        /// <returns>Url office</returns>
        private async Task<string> GetUrlOffice(string token, int idContainer, int idFile, string linkOffice)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this._urlTenant + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var urlResponse = await client.GetAsync($"{linkOffice}/{idContainer}/{idFile}/{token}/view");
                if (urlResponse.IsSuccessStatusCode)
                {
                    string dataUrl = await urlResponse.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<string>(dataUrl);
                }
            }

            throw new Exception("Error get url office");
        }

        /// <summary>
        /// Get url viewer document no office
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idContainer">Identifier container (box or file container)</param>
        /// <param name="idFile">Idnetifier file</param>
        /// <param name="isFileContainer">Indicate file is in file container</param>
        /// <returns>Url viewer no office</returns>
        private async Task<string> ViewerGDP(string token, int idContainer, int idFile, bool isFileContainer)
        {
            throw new Exception("Not open viewer");

        }
        #endregion
    }

    public class FileContainerMetadata
    {

        [JsonProperty("idMetadata")]
        public int idMetadata { get; set; }

        [JsonProperty("idFileItem")]
        public int idFileItem { get; set; }

        [JsonProperty("nameToShow")]
        public string nameToShow { get; set; }

        [JsonProperty("versionNumber")]
        public int versionNumber { get; set; }

        [JsonProperty("idValueList")]
        public int idValueList { get; set; }

        [JsonProperty("intValue")]
        public int? intValue { get; set; }

        [JsonProperty("decimalValue")]
        public decimal? decimalValue { get; set; }

        [JsonProperty("dateTimeValue")]
        public string dateTimeValue { get; set; }

        [JsonProperty("stringValue")]
        public string stringValue { get; set; }

        [JsonProperty("valueBit")]
        public bool? valueBit { get; set; }

    }

}

