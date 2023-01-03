using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.ServiceProcess;
using System.Configuration;

using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SelectionMode = System.Windows.Controls.SelectionMode;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System;
using System.Xaml;

namespace ProjectMichalWendt
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ServiceController serviceController;
        static HashSet<string> archivedFiles = new HashSet<string>(); // I hold only unique paths to files
        public static string SelectedPath = "c:\\archives";  // Path to archivized files

        readonly static string EventLogName = ConfigurationManager.AppSettings["Dziennik"];
        readonly static string EventLogSource = ConfigurationManager.AppSettings["Zrodlo"];
        readonly static string ServiceName = ConfigurationManager.AppSettings["ServiceName"];
        readonly static string ConfigDirectoryPath = ConfigurationManager.AppSettings["ConfigDirectoryPath"];

        public MainWindow()
        {
            //MessageBox.Show("log");
            InitializeComponent();
            archivedFilesWindow.SelectionMode = SelectionMode.Multiple; // Let user choose multiple files from the list to delete
            SelectedPathTxtBox.Text = SelectedPath; // Default path to save archives


            if (ServiceController.GetServices().Any(serviceController =>
                 serviceController.ServiceName.Equals("ProjectService")) == true)
            {
                serviceController = new ServiceController("ProjectService");


                //var config = ConfigurationManager.OpenExeConfiguration(@".\ServiceMichalWendt.exe");
                //FolderBox.Text = config.AppSettings.Settings["Sciezka"].Value;

                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    StartButton.IsEnabled = false;
                    label.Content = "Usługa: Uruchomiona";
                }
                else if (serviceController.Status == ServiceControllerStatus.Stopped)
                {
                    StopButton.IsEnabled = false;
                    label.Content = "Usługa: Zatrzymana";
                }
            }
            else
            {
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = false;
                label.Content = "Usługa: Nie istnieje";
            }
            Directory.CreateDirectory(ConfigDirectoryPath);
        }

        private void refreshList()
        {
            archivedFilesWindow.ItemsSource = null; // ListBox needs to be set null for proper refresh 
            archivedFilesWindow.ItemsSource = archivedFiles;
        }

        private void Add_File(object sender, RoutedEventArgs e) // Add new file/files to list
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();   // Opens file explorer
            openFileDialog.Multiselect = true; // Enable to select more than one file
            //openFileDialog.InitialDirectory = @"c:\"; // Opens file explorer at specific folder
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    archivedFiles.Add(filename);
                }
                //txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
            }
            refreshList();
        }

        private void Remove_File(object sender, RoutedEventArgs e)  // Delete file from the list
        {
            foreach (string item in archivedFilesWindow.SelectedItems)  // Remove each of selected paths from list
            {
                archivedFiles.Remove(item);
            }
            refreshList();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)   // Button for archivization folder choosing
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                SelectedPathTxtBox.Text = openFileDlg.SelectedPath;
            }
            SelectedPath = SelectedPathTxtBox.Text;
        }

        #region Dependency Properties

        public static readonly DependencyProperty SelectedPathProperty =
            DependencyProperty.Register(
            "SelectedPath",
            typeof(string),
            typeof(MainWindow),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(SelectedPathChanged))
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

        #endregion             

        private void SelectedPathTxtBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)   // On user input in text box change
        {
            SelectedPath = SelectedPathTxtBox.Text;
        }

        private static void SelectedPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)   // On change of path to saved files
        {
            ((MainWindow)d).SelectedPathTxtBox.Text = e.NewValue.ToString();
        }

        private void Archivize()    // THIS METHOD IS IN LIBRARY PROJECT (HERE ONLY FOR TESTING)
        {
            Directory.CreateDirectory(SelectedPath);
            // Create and open a new ZIP file
            using (ZipArchive zip = ZipFile.Open(SelectedPath + "\\archive.zip", ZipArchiveMode.Update))
            {
                foreach (var file in archivedFiles)
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }                      
        }

        public void SaveListToFile()    // Hold current files and archiv folder paths in files
        {
            string path = ConfigDirectoryPath + "archivedPath.txt"; // Save archive path to txt file using plain text
            File.WriteAllText(path, SelectedPath);


            string path2 = ConfigDirectoryPath + "archivedFiles.txt";   // Save files names to txt file using plain text
            File.WriteAllText(path2, String.Join("\n", archivedFiles.ToArray()));

            /*using (StreamWriter writer = File.CreateText(ConfigDirectoryPath + "archivedFiles.txt")) // Save archive path to txt file using Xml
            {
                XamlServices.Save(writer, SelectedPath);
            }
            using (StreamWriter writer = File.AppendText(ConfigDirectoryPath + "archivedPath.txt")) // Save files names to txt file using Xml
            {
                XamlServices.Save(writer, archivedFiles);
            }*/
        }

        public void StartButton_Click(object sender, RoutedEventArgs e) // Start app but ensure if archiv folder was choosen
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            AddButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            FolderButton.IsEnabled = false;
            label.Content = "Usługa: Uruchomiona";

            if (SelectedPathTxtBox.Text == @"c:\archives")
            {
                MessageBoxResult result = MessageBox.Show("Sciezka zapisu nie zostala ustawiona.\nDomyślnie wybrana: " + SelectedPathTxtBox.Text + "\nCzy chcesz ją zmienić?", "Nie podano sciezki", MessageBoxButton.YesNo, MessageBoxImage.Question);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        break;
                    case MessageBoxResult.No:
                        SaveListToFile();
                        serviceController.Start();
                        serviceController.WaitForStatus(ServiceControllerStatus.Running);                      
                        break;
                }
            }
            else
            {
                SaveListToFile();
                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running);

            }
        }

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            serviceController.Stop();
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);

            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            AddButton.IsEnabled = true;
            DeleteButton.IsEnabled = true;
            FolderButton.IsEnabled = true;
            label.Content = "Usługa: Zatrzymana";
        }
    }
}
