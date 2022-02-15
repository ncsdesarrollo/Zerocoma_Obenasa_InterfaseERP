using Kyocera.Solpheo.ApiClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolucionFacturasComunes.Models
{
    public class Items
    {
        public int? id { get; set; }
        public int? idFileContainer { get; set; }
        public string name { get; set; }
        public DateTime createdDate { get; set; }
        public DateTime updatedDate { get; set; }
        public string route { get; set; }
        public string owner { get; set; }
        public int? idParentFileItem { get; set; }
        public int? idFileContainerPath { get; set; }
        public int? state { get; set; }
        public bool? locked { get; set; }
        public string lockedBy { get; set; }
        public int? bytesSpace { get; set; }
        public string extension { get; set; }
        public int? versionNumber { get; set; }
        public int? volume { get; set; }
        public int? position { get; set; }
        public string type { get; set; } = "fileItem";
    }

    public class FileItems
    {
        public IEnumerable<FileContainerListViewModel> items { get; set; }

        public int idWorkflowActivity { get; set; }
        public int resultKey { get; set; }
        public bool notCheckRequiredVariables { get; set; }
        public bool replaceFile { get; set; }
        public string content { get; set; }
        public object signType { get; set; }
    }

    public class ResultIdFileContainer
    {
        public int IdFileContainer { get; set; }


    }

    public class WorkflowActivityViewModel
    {
        public bool Resultado { get; set; }
        public string Id { get; set; }
        public string TaskKey { get; set; }
    }
}
