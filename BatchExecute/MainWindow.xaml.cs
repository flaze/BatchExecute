using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BatchExecute
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string[] _validExtensions;
        private DProgram _currentProgram;
        private ExecuteThread _executeThread;

        public DProgram CurrentProgram
        {
            get { return _currentProgram; }
            set
            {
                _currentProgram = value;
                FirePropertyChanged("CurrentProgram");
            }
        }

        public ObservableCollection<DFile> Files { get; set; }
        public ObservableCollection<DProgram> Programs { get; set; }

        public MainWindow()
        {
            _executeThread = new ExecuteThread(this);
            _executeThread.Complete += _executeThread_Complete;
            _executeThread.Stopped += _executeThread_Stopped;

            Files = new ObservableCollection<DFile>();
            Programs = new ObservableCollection<DProgram>();

            DataContext = this;
            InitializeComponent();

            loadSettings();

            if(Programs.Count > 0)
                lvPrograms.SelectedIndex = 0;
        }

        #region Settings

        private void loadSettings()
        {
            loadPrograms();
            tbFileExtensions.Text = Properties.Settings.Default.Extensions;

            if (Properties.Settings.Default.WindowMode == "Hidden")
                rbWindowHidden.IsChecked = true;
            else if (Properties.Settings.Default.WindowMode == "Minimized")
                rbWindowMinimized.IsChecked = true;
            else
                rbWindowNormal.IsChecked = true;
        }

        private void saveSettings()
        {
            savePrograms();
            Properties.Settings.Default.Extensions = tbFileExtensions.Text;
            Properties.Settings.Default.WindowMode = getWindowMode();

            Properties.Settings.Default.Save();
            Debug.WriteLine("Saved");
        }

        private void loadPrograms()
        {
            string[] lines = Properties.Settings.Default.Programs.Replace("\r", "").Split('\n');
            foreach (var line in lines)
            {
                if (line != "")
                {
                    var part = line.Split('|');
                    if (part.Length == 3)
                    {
                        Programs.Add(new DProgram(fromSafeString(part[0]), fromSafeString(part[1]), fromSafeString(part[2])));
                    }
                }
            }
        }

        private void savePrograms()
        {
            StringBuilder sb = new StringBuilder();
            foreach (DProgram program in Programs)
            {
                sb.Append(toSafeString(program.Name) + "|" + toSafeString(program.Filename) + "|" + toSafeString(program.Arguments) + '\n');
            }
            Properties.Settings.Default.Programs = sb.ToString();
        }

        private string getWindowMode()
        {
            if (rbWindowHidden.IsChecked == true)
                return "Hidden";
            else if (rbWindowMinimized.IsChecked == true)
                return "Minimized";
            
            return "Normal";
        }

        private string toSafeString(string value)
        {
            return value.Replace("|", "\\|");
        }

        private string fromSafeString(string value)
        {
            return value.Replace("\\|", "|");
        }

        #endregion

        #region Load Path

        private void loadPath(string path)
        {
            _validExtensions = tbFileExtensions.Text.Split(' ');

            if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                loadDirectory(new DirectoryInfo(path));
            }
            else
            {
                loadFile(new FileInfo(path));
            }
        }

        private void loadDirectory(DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles())
            {
                loadFile(file);
            }

            foreach (DirectoryInfo dir in directory.GetDirectories())
            {
                loadDirectory(dir);
            }
        }

        private void loadFile(FileInfo file)
        {
            var fileExt = file.Extension.Substring(1);
            if (_validExtensions.Contains(fileExt))
            {
                Files.Add(new DFile(file));
            }
            else
            {
                Debug.WriteLine(string.Format("Invalid Extension Match {0} not in {1}", fileExt, tbFileExtensions.Text));
            }
        }

        #endregion

        #region Event Handlers

        private void lvFiles_Drop(object sender, DragEventArgs e)
        {
            if(e.Data == null) return;
            
            DataObject data = (DataObject)e.Data;
            if(!data.ContainsFileDropList()) return;

            var files = data.GetFileDropList();

            foreach (string path in files)
            {
                loadPath(path);
            }

            Debug.WriteLine("lvFiles_Drop");
        }

        private void lvFiles_PreviewDragEnter(object sender, DragEventArgs e)
        {
            var valid = e.Data != null && ((DataObject)e.Data).ContainsFileDropList();
            if (valid)
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private void lvFiles_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void btnAddProgram_Click(object sender, RoutedEventArgs e)
        {
            var program = new DProgram();
            Programs.Add(program);
            lvPrograms.SelectedItem = program;
        }

        private void btnRemoveProgram_Click(object sender, RoutedEventArgs e)
        {
            if (lvPrograms.SelectedIndex < 0 || lvPrograms.SelectedIndex >= Programs.Count)
                return;

            var program = Programs[lvPrograms.SelectedIndex];

            if (MessageBox.Show(string.Format("Are you sure you want to remove \"{0}\"?", program.Name),
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                Programs.RemoveAt(lvPrograms.SelectedIndex);
            }
        }

        private void lvPrograms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                CurrentProgram = e.AddedItems[0] as DProgram;

                tbProgramName.Text = CurrentProgram.Name;
                tbProgramFilename.Text = CurrentProgram.Filename;
                tbProgramArguments.Text = CurrentProgram.Arguments;
            }
            else
            {
                tbProgramName.Text = "";
                tbProgramFilename.Text = "";
                tbProgramArguments.Text = "";
            }
        }

        private void tbProgramName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentProgram == null) return;

            CurrentProgram.Name = tbProgramName.Text;
        }

        private void tbProgramFilename_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentProgram == null) return;

            CurrentProgram.Filename = tbProgramFilename.Text;
        }

        private void tbProgramArguments_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentProgram == null) return;

            CurrentProgram.Arguments = tbProgramArguments.Text;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            saveSettings();
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            CurrentProgram.IsRunning = true;
            lvFiles.IsEnabled = false;
            _executeThread.Execute(Files, CurrentProgram, getWindowMode());
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to cancel the current process?",
                "Confirm Process Cancellation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                _executeThread.Stop();
            }
        }

        private void _executeThread_Complete()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                CurrentProgram.IsRunning = false;
                lvFiles.IsEnabled = true;
            }));
        }

        private void _executeThread_Stopped()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                foreach (DFile file in Files)
                {
                    file.State = "Pending";
                }
            }));
        }

        private void btnProgramFilenameBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            if (dialog.ShowDialog() == true)
            {
                tbProgramFilename.Text = dialog.FileName;
            }
        }

        private void btnClearFiles_Click(object sender, RoutedEventArgs e)
        {
            Files.Clear();
        }

        #endregion

        internal void UpdateState(int index, string state)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                Files[index].State = state;
            }));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
