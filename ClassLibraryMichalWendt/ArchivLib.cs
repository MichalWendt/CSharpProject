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
using System.Xml.Linq;

namespace ClassLibraryMichalWendt
{
    public class ArchivLib
    {        
        private TraceSwitch traceSwitch;
        private EventLog eventLog;
        private string workingDirectory;
        private string eventLogName;
        private string sourceName;
        
        private FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
        public static List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();


        public void StartService()
        {           
            List<string> archivedFiles = new List<string>();
            string ConfigDirectoryPath;
            sourceName = ConfigurationManager.AppSettings.Get("Zrodlo");
            eventLogName = ConfigurationManager.AppSettings.Get("Dziennik");
            ConfigDirectoryPath = ConfigurationManager.AppSettings.Get("ConfigDirectoryPath");
            string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffffffZ");
            string zipSelectedPath = File.ReadAllText(ConfigDirectoryPath + "archivedPath.txt").Split('\n')[0];

            workingDirectory = ConfigurationManager.AppSettings.Get("Sciezka");
            if (!EventLog.SourceExists(sourceName, "."))
            {
                EventLog.CreateEventSource(sourceName, eventLogName);
            }
            eventLog = new EventLog(eventLogName, ".", sourceName);

            archivedFiles = ReadListFromFile(ConfigDirectoryPath, "archivedPath.txt", "archivedFiles.txt", zipSelectedPath);
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
                        UpdateArchive(archivedFiles, date, zipSelectedPath);    // Update archive on choosen files change
                    };
                }
                if (traceSwitch.TraceWarning)
                {
                    watchers[i].Renamed += (object sender, RenamedEventArgs e) =>
                    {
                        eventLog.WriteEntry(e.Name + " :renamed\n", EventLogEntryType.Information);
                        archivedFiles.Remove(e.Name);   // Remove file from list if it was deleted
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
        }         

        public void UpdateArchive(List<string> archivedFiles, String date, string zipSelectedPath) // Update existing zip
        {
            Directory.CreateDirectory(zipSelectedPath);
            // Create and open a new ZIP file

            using (ZipArchive archive = ZipFile.Open(zipSelectedPath + "\\archive" + date + ".zip", ZipArchiveMode.Update))
            {
                do
                {
                    archive.Entries[0].Delete();
                } while (archive.Entries.Count > 0);

                foreach (var file in archivedFiles)
                {
                    archive.CreateEntryFromFile(file, System.IO.Path.GetFileName(file));
                    //archive.ExtractToDirectory(extractPath);
                }
            }
        }

        public List<string> ReadListFromFile(String ConfigDirectoryPath, String archivedPath, String archivedFilesPath, string zipSelectedPath)  // Read paths saved in configuration files
        {
            List<string> archivedFiles = new List<string>();
            string path = ConfigDirectoryPath + archivedPath;    // Read archive path from txt file using plain text
            
            //eventLog.WriteEntry("zipSelectedPath" + zipSelectedPath, EventLogEntryType.Error);
            string path2 = ConfigDirectoryPath + archivedFilesPath;   // Read file names from txt file using plain text
            List<string> result = File.ReadAllText(path2).Split('\n').ToList();
            foreach (string file in result)
            {
                archivedFiles.Add(file);
            }
            return archivedFiles;
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
            for (int i = 0; i < watchers.Count; ++i)
            {
                watchers[i].Dispose();
            }
            eventLog.Dispose();
        }        
    }
}
