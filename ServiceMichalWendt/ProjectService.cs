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
using ClassLibraryMichalWendt;

namespace ServiceMichalWendt
{
    internal partial class ProjectService : ServiceBase
    {
        private ArchivLib archivLib = new ArchivLib();

        public ProjectService()
        {
            //InitializeComponent();
            this.ServiceName = ConfigurationManager.AppSettings.Get("ServiceName");
        }

        protected override void OnStart(string[] args)
        {
            archivLib.StartService();
        }

        protected override void OnStop()
        {
            archivLib.StopService();
        }
    }
}
