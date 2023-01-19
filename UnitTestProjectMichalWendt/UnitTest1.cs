using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ClassLibraryMichalWendt;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xaml;
using System.Diagnostics;

namespace UnitTestProjectMichalWendt.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod()]
        public void UpdateArchiveDirectoryCreatedTest()
        {
            string zipSelectedPath = "c:\\archives";
            ArchivLib t = new ArchivLib();
            Assert.IsTrue(Directory.Exists(zipSelectedPath), "Directory was created succesfuly on Update");   // Check if directory was created
        }

        [TestMethod()]
        public void UpdateArchiveFilesAddedTest()
        {
            string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffffffZ");
            List<string> archivedFiles = new List<string>();
            List<string> archivedFiles2 = new List<string>();

            string path = "" + "UnitTest1.cs";   // Save files names to txt file using plain text
            File.WriteAllText(path, "test");

            archivedFiles.Add(path);
            string zipSelectedPath = "c:\\archives";

            using (ZipArchive archive = ZipFile.Open(zipSelectedPath + "\\archive" + date + ".zip", ZipArchiveMode.Create)) // Archive all files from list
            {
                foreach (var file in archivedFiles)
                {
                    archive.CreateEntryFromFile(file, System.IO.Path.GetFileName(file));
                }
            }

            ArchivLib t = new ArchivLib();
            t.UpdateArchive(archivedFiles, date, zipSelectedPath);
            using (ZipArchive archive = ZipFile.OpenRead(zipSelectedPath + "\\archive" + date + ".zip"))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    archivedFiles2.Add(entry.FullName);
                }
            }
            Assert.AreEqual(archivedFiles2[0], archivedFiles[0], "Files from zip archive are the same as saved ones");
        }

        [TestMethod()]
        public void EditedFileArchivizedDirectoryCreatedTest()
        {
            List<string> archivedFiles = new List<string>();
            archivedFiles.Add("\\UnitTest1.cs");
            string zipSelectedPath = "c:\\archives";
            ArchivLib t = new ArchivLib();
            Assert.IsTrue(Directory.Exists(zipSelectedPath), "Directory was created succesfuly on Edit");
        }

        [TestMethod()]
        public void ReadListFromFileTest()
        {
            string zipSelectedPath = "c:\\archives";

            List<string> archivedFiles = new List<string>();
            archivedFiles.Add("UnitTest1.cs");

            string path2 = "archivedFiles.txt";   // Save files names to txt file using plain text
            File.WriteAllText(path2, String.Join("\n", archivedFiles.ToArray()));

            ArchivLib t = new ArchivLib();
            List<string> list = t.ReadListFromFile("", "archivedPath.txt", "archivedFiles.txt", zipSelectedPath);
            Assert.AreEqual(archivedFiles[0], list[0], "List from file is equal to List<string>");
        }

        [TestMethod()]
        public void StopNotStartedServiceTest()
        {
            ArchivLib t = new ArchivLib();
            Assert.ThrowsException<NullReferenceException>(() => t.StopService(), "Service throws exception on stop when is not started");
        }
    }
}
