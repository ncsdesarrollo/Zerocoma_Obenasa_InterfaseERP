using Newtonsoft.Json;
using SolucionFacturasComunes.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolucionFacturasLauncher
{
    public class FacturasLauncherConfig
    {
        public Configuracion Configuracion { get; set; }
        public string LogTenant { get; set; }
        public FacturasLauncherConfig(string tenant)
        {
            string data = File.ReadAllText(ConfigurationManager.AppSettings["RutaJSONConfiguracion"] + @"\" + tenant + ".json"); // el nombre del json es el del tenant recibido por parámetro, por ejemplo solpheo.json
            Configuracion = JsonConvert.DeserializeObject<Configuracion>(data);
            LogTenant = ConfigurationManager.AppSettings["RutaLogs"] + @"\" + tenant + ".log";         
        }

        private string GetFromConfig(string key, string defaultValue)
        {
            string valueFromConfig = ConfigurationManager.AppSettings[key];

            return valueFromConfig;
        }
    }
}
