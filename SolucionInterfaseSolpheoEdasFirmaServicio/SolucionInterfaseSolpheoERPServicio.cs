using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using SolucionFacturasLauncher;

namespace SolucionFacturasServicio
{
    public partial class SolucionInterfaseSolpheoERPServicio : ServiceBase
    {
        string Tenant = String.Empty;
        public SolucionInterfaseSolpheoERPServicio(string[] args)
        {
            InitializeComponent();

            if (args.Length > 0)
                Tenant = args[0];
            Tenant = "solpheo";
            if (ConfigurationManager.AppSettings["ModoDepuracion"].ToString() == "1") {
                FacturasLauncher launcher = new FacturasLauncher();
                launcher.Start(Tenant);
            }

        }

        FacturasLauncher launcher = new FacturasLauncher();

        protected override void OnStart(string[] args)
        { 
            if (args.Length > 0)
            {
                Tenant = args[0];
            }
            launcher.Start(Tenant);
        }
        protected override void OnStop()
        {
            launcher.Stop();
        }
    }
}
