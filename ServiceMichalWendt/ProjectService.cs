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
            try
            {
                archivLib.StartService();
            } catch (Exception ex)
            {
                EventLog.WriteEntry("Service", "Failed to start Service " + ex.Message, EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            try
            {
                archivLib.StopService();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Service", "Failed to stop Service " + ex.Message, EventLogEntryType.Error);
            }

        }
    }
}
