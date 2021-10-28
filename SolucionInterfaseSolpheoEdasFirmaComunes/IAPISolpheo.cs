using Kyocera.Solpheo.ApiClient.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolucionFacturasComunes
{
    interface IAPISolpheo
    {

        /// <summary>
        /// Method to get access token, login Solpheo Web
        /// </summary>
        /// <param name="user">User login</param>
        /// <param name="pass">Password login</param>
        /// <param name="tenant">Tenant</param>
        /// <param name="type">Type client id (roclient)</param>
        /// <param name="secret">Secret client (secret)</param>
        /// <returns>Login information</returns>
        Task<Login> LoginAsync(string user, string pass, string tenant, string type, string secret);

        /// <summary>
        /// Revocation token user
        /// </summary>
        /// <param name="token">Token to revocate</param>
        /// <returns>True if revocation success, false otherwise</returns>
        Task<bool> LogoutAsync(string token);

        /// <summary>
        /// Get boxes workplace
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <param name="personal">Indicate if we only want the personal drive </param>
        /// <returns>List boxes paged</returns>
        Task<PagedList<Boxes>> BoxesAsync(string token, int page, int pageSize, bool personal = false);

        /// <summary>
        /// Get folders and documents of a box
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idbox">Identifier box</param>
        /// <param name="idfolder">Identifier folder</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <returns>List items paged</returns>
        Task<PagedList<ItemBox>> FoldersAsync(string token, int idbox, int idfolder, int page, int pageSize);

        /// <summary>
        /// Create a new folder
        /// </summary>
        /// <param name="token"></param>
        /// <param name="idbox"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="idFolderParent"></param>
        /// <returns>Identifier new folder</returns>
        Task<ResultId> CreateFolderAsync(string token, int idbox, string name, string description, int idFolderParent);

        /// <summary>
        /// Upload a new document in a box
        /// </summary>
        /// <param name="idbox">Identifier box</param>
        /// <param name="token">Access token</param>
        /// <param name="namefile">Name file</param>
        /// <param name="file">File in bytes</param>
        /// <param name="idFolder">Identifier folder</param>
        /// <returns>Identifier new document</returns>
        Task<ResultId> UploadDocumentAsync(int idbox, string token, string namefile, byte[] file, int idFolder);

        /// <summary>
        /// Download a folder
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="id">Identifier folder</param>
        /// <param name="name">Name file download</param>
        /// <param name="ext">Extension file, default zip</param>
        /// <returns>Information file downloaded</returns>
        Task<FileDownload> DownloadFolderAsync(string token, int id, string name, string ext = "zip");

        /// <summary>
        /// Download a folder
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="id">Identifier file</param>
        /// <param name="name">Name file download</param>
        /// <param name="ext">Extension file, default zip</param>
        /// <returns>Information file downloaded</returns>
        Task<FileDownload> DownloadDocumentsync(string token, int id, string name, string ext);

        /// <summary>
        /// Get tree level
        /// </summary>
        /// <param name="token">Access token</param>
        /// <returns>List levels</returns>
        Task<IEnumerable<LevelItemViewModel>> Levels(string token);

        /// <summary>
        /// Get file all file containers
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <returns>File containers paged</returns>
        Task<PagedList<FileContainerListViewModel>> FileContainersAsync(string token, int page, int pageSize);

        /// <summary>
        /// Get file items (documents) file container
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFilecontainer">Identifier file container</param>
        /// <param name="page">Page pagination</param>
        /// <param name="pageSize">Number items by page</param>
        /// <returns>Items paged</returns>
        Task<PagedList<FileContainerListViewModel>> FileItemsAsync(string token, int idFilecontainer, int page, int pageSize);

        /// <summary>
        /// Get metadatas of a container
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFileContainer">Identifier file container</param>
        /// <returns>Metadatas paged</returns>
        Task<PagedList<MetadataFileItemValue>> MetadatasFileContainerAsync(string token, int idFileContainer);

        /// <summary>
        /// Get metadatas of a file item
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFileContainer">Identifier file container</param>
        /// <param name="idFileItem">Identifier file item</param>
        /// <returns>Metadatas file item paged</returns>
        Task<PagedList<MetadataFileItemValue>> MetadatasFileItemAsync(string token, int idFileContainer, int idFileItem);

        /// <summary>
        /// Update metadatas file item
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idFileContainer">Identifier file container</param>
        /// <param name="metadatas">Metadtas to update</param>
        /// <returns>Task empty</returns>
      
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
        Task<ResultId> UploadFileItemAsync(string token, int idFileContainer, string nameFile, byte[] file, FileContainerMetadataValue[] metadatas, bool isRecord);

        /// <summary>
        /// Get url viewer
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="idContainer">Identifier container</param>
        /// <param name="idFile">Identifier file</param>
        /// <param name="isFileContainer">Indicate if container is file container</param>
        /// <param name="extension">Extension file</param>
        /// <returns>Url viewer</returns>
        Task<string> Viewer(string token, int idContainer, int idFile, bool isFileContainer, string extension);
    }
}
