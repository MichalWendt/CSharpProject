using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Xaml;

namespace ClassLibraryMichalWendt
{
    public class ArchivLib
    {
        List<string> archivedFiles = new List<string>();
        string zipSelectedPath = "";

        //private System.Timers.Timer _timer;
        //private DateTime _lastRun = DateTime.Now.AddDays(-1);

        private TraceSwitch traceSwitch;
        private EventLog eventLog;
        private string workingDirectory;
        private string ConfigDirectoryPath;
        private string eventLogName;
        private string sourceName;
        private string date;
        private FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
        public static List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        public void StartService()
        {
            

            sourceName = ConfigurationManager.AppSettings.Get("Zrodlo");
            eventLogName = ConfigurationManager.AppSettings.Get("Dziennik");
            ConfigDirectoryPath = ConfigurationManager.AppSettings.Get("ConfigDirectoryPath");
            date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffffffZ");
                        
            
            workingDirectory = ConfigurationManager.AppSettings.Get("Sciezka");
            if (!EventLog.SourceExists(sourceName, "."))
            {
                EventLog.CreateEventSource(sourceName, eventLogName);
            }
            eventLog = new EventLog(eventLogName, ".", sourceName);

            eventLog.WriteEntry("1", EventLogEntryType.Error);

            ReadListFromFile();
            traceSwitch = new TraceSwitch("Logowanie", "Level of loging done on directory");


            foreach(var file in archivedFiles)  // File watchers for all choosen files
            {
                fileSystemWatcher = new FileSystemWatcher();
                fileSystemWatcher.Path = System.IO.Path.GetDirectoryName(file);
                fileSystemWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
                fileSystemWatcher.Filter = System.IO.Path.GetFileName(file);
                fileSystemWatcher.IncludeSubdirectories = true;
                watchers.Add(fileSystemWatcher);
            }

            for (int i = 0; i < watchers.Count; ++i)
            {
                if (traceSwitch.TraceInfo)
                {
                    watchers[i].Changed += (Object sender, FileSystemEventArgs e) =>
                    {
                        eventLog.WriteEntry(e.Name + " :changed\n", EventLogEntryType.Information);
                        UpdateArchive();    // Update archive on choosen files change
                    };
                }
                if (traceSwitch.TraceWarning)
                {
                    watchers[i].Renamed += (object sender, RenamedEventArgs e) =>
                    {
                        eventLog.WriteEntry(e.Name + " :renamed\n", EventLogEntryType.Information);
                    };
                }
                if (traceSwitch.TraceError)
                {
                    watchers[i].Created += (Object sender, FileSystemEventArgs e) =>
                    {
                        eventLog.WriteEntry(e.Name + " :created\n", EventLogEntryType.Information);    // Can't happen but wont couse problems

                    };
                    watchers[i].Deleted += (Object sender, FileSystemEventArgs e) =>
                    {
                        eventLog.WriteEntry(e.Name + " :deleted\n", EventLogEntryType.Information);
                        archivedFiles.Remove(e.Name);   // Remove file from list if it was deleted
                    };
                }
                watchers[i].EnableRaisingEvents = true;
            }


            // Creating new zip file ________________________________________________________________________

            Directory.CreateDirectory(zipSelectedPath); // Create folder if does not exists (it's not nesesery to check if it exists)
            // Create and open a new ZIP file
            using (ZipArchive archive = ZipFile.Open(zipSelectedPath + "\\archive" + date + ".zip", ZipArchiveMode.Create)) // Archive all files from list
            {
                foreach (var file in archivedFiles)
                {
                    archive.CreateEntryFromFile(file, System.IO.Path.GetFileName(file));
                    //archive.ExtractToDirectory(extractPath);
                }
            }

            // Timers not used for now
            /*
            _timer = new System.Timers.Timer(10 * 60 * 1000); // every 10 minutes
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            _timer.Start();
            */
        }
            
        /*
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) // Method for counting elapsed time
        {
            // ignore the time, just compare the date
            if (_lastRun.Date < DateTime.Now.Date)
            {
                // stop the timer while we are running the cleanup task
                _timer.Stop();
                UpdateArchive();
                _lastRun = DateTime.Now;
                _timer.Start();
            }
        }
        */

        public void UpdateArchive() // Update existing zip
        {
            Directory.CreateDirectory(zipSelectedPath);
            // Create and open a new ZIP file
            using (ZipArchive archive = ZipFile.Open(zipSelectedPath + "\\archive" + date + ".zip", ZipArchiveMode.Update))
            {
                foreach (var file in archivedFiles)
                {
                    archive.CreateEntryFromFile(file, System.IO.Path.GetFileName(file));
                    //archive.ExtractToDirectory(extractPath);
                }
            }
        }

        public void ReadListFromFile()  // Read paths saved in configuration files
        {
            string path = ConfigDirectoryPath + "archivedPath.txt";    // Read archive path from txt file using plain text
            zipSelectedPath =  File.ReadAllText(path).Split('\n')[0];
            eventLog.WriteEntry("zipSelectedPath" + zipSelectedPath, EventLogEntryType.Error);
            string path2 = ConfigDirectoryPath + "archivedFiles.txt";   // Read file names from txt file using plain text
            List<string> result = File.ReadAllText(path2).Split('\n').ToList();
            foreach (string file in result)
            {
                archivedFiles.Add(file);
            }

            /*using (StreamReader tr = new StreamReader(ConfigDirectoryPath + "archivedFiles.txt"))  // Read archive path from txt file using Xml
            {                
                List<string> result = (List<string>)XamlServices.Load(tr);
                EventLog.WriteEntry("Service", "Path: " + result, EventLogEntryType.Warning);
                zipSelectedPath = result[0];
            }
            using (StreamReader tr = new StreamReader(ConfigDirectoryPath + "archivedPath.txt"))  // Read file names from txt file using Xml
            {
                List<string> result = (List<string>)XamlServices.Load(tr);
                foreach (string path in result)
                {
                    EventLog.WriteEntry("Service", "File: " + result, EventLogEntryType.Warning);
                    archivedFiles.Add(path);
                }
            }*/
        }

        public void StopService()   // On stop button pushed disables watchers and logs
        {
            /*for (int i = 0; i < watchers.Count; ++i)
            {
                watchers[i].Dispose();
            }*/
            eventLog.Dispose();
        }

        /*public delegate void FileSystemWatcherEventHandler(object sender, FileSystemEventArgs args);

        public event FileSystemWatcherEventHandler OnChange;
        public DateTime LastFired { get; private set; }

        public void FileSystemWatcher(string path)
        {
            Changed += HandleChange;
            Created += HandleChange;
            Deleted += HandleChange;
            LastFired = DateTime.MinValue;
        }

        private void GeneralChange(object sender, FileSystemEventArgs args)
        {
            if (LastFired.Add(TimeSpan.FromSeconds(5)) < DateTime.UtcNow)
            {
                OnChange.Invoke(sender, args);
                LastFired = DateTime.UtcNow;
            }
        }

        private void HandleChange(object sender, FileSystemEventArgs args)
        {
            GeneralChange(sender, args);
        }

        protected override void Dispose(bool disposing)
        {
            OnChange = null;
            base.Dispose(disposing);
        }

        public void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                //Logging.Write_To_Log_File("Item change detected " + e.ChangeType + " " + e.FullPath + " " + e.Name, MethodBase.GetCurrentMethod().Name, "", "", "", "", "", "", 2);
                watchers.Clear();

                foreach (FileSystemWatcher element in watchers)
                {
                    element.EnableRaisingEvents = false;
                }

                //Do some processing on my list of files here....
                return;

            }
            catch (Exception ex)
            {
                // If exception happens, it will be returned here
            }
            finally
            {
                foreach (FileSystemWatcher element in watchers)
                {
                    element.EnableRaisingEvents = true;
                }
            }
        }

        public void UpdateWatcher() // Check Items
        {

            try
            {
                watchers.Clear();

                for (int i = 0; i < archivedFiles.Count; i++) // Loop through List with for
                {
                    FileSystemWatcher w = new FileSystemWatcher();
                    w.Path = System.IO.Path.GetDirectoryName(archivedFiles[i]); // File path    
                    w.Filter = System.IO.Path.GetFileName(archivedFiles[i]); // File name
                    w.Changed += new FileSystemEventHandler(OnChanged);
                    w.Deleted += new FileSystemEventHandler(OnChanged);
                    w.Created += new FileSystemEventHandler(OnChanged);
                    w.EnableRaisingEvents = true;
                    watchers.Add(w);
                }
            }
            catch (Exception ex)
            {
                // If exception happens, it will be returned here

            }
        }*/
    }
}
