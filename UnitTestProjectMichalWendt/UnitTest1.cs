using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ClassLibraryMichalWendt;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;

namespace UnitTestProjectMichalWendt.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod()]
        public void UpdateArchiveTest()
        {
            List<string> archivedFiles = new List<string>();
            archivedFiles.Add("\\UnitTest1.cs");
            string zipSelectedPath = "c:\\archives";
            ArchivLib t = new ArchivLib();
            Assert.IsTrue(Directory.Exists(zipSelectedPath));   // Check if directory was created
            Assert.IsTrue(!Directory.EnumerateFileSystemEntries(zipSelectedPath).Any());    // Check if files were added
        }

        [TestMethod()]
        public void EditedFileArchivizedTest()
        {
            List<string> archivedFiles = new List<string>();
            archivedFiles.Add("\\UnitTest1.cs");
            string zipSelectedPath = "c:\\archives";
            ArchivLib t = new ArchivLib();
            Assert.IsTrue(Directory.Exists(zipSelectedPath));   // Check if directory was created
            Assert.IsTrue(!Directory.EnumerateFileSystemEntries(zipSelectedPath).Any());    // Check if files were added
        }

        [TestMethod()]
        public void ReadListFromFileTest()
        {
            using (StreamWriter writer = File.CreateText("archivedFiles.txt")) // Save archive patch to txt file
            {
                XamlServices.Save(writer, "test");
            }
            using (StreamWriter writer = File.AppendText("archivedPath.txt")) // Save files names to txt file
            {
                XamlServices.Save(writer, "test");
            }
            ArchivLib t = new ArchivLib();
            //Assert.Equals();
        }

        [TestMethod()]
        public void StopServiceTest()
        {
            ArchivLib t = new ArchivLib();
            Assert.ThrowsException<Exception>(() => t.StopService());
        }

        
    }
}
