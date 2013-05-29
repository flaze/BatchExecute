using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;

namespace BatchExecute
{
    public partial class PreviewWindow
    {
        public DProgram CurrentProgram
        {
            get { return _mainWindow.CurrentProgram; }
        }

        public ObservableCollection<DFile> Files
        {
            get { return _mainWindow.Files; }
        }

        public ObservableCollection<DProgram> Programs
        {
            get { return _mainWindow.Programs; }
        }

        public ObservableCollection<PreviewItem> Results { get; set; }

        private readonly MainWindow _mainWindow;

        public PreviewWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            DataContext = this;

            Results = new ObservableCollection<PreviewItem>();
            Update();

            InitializeComponent();
        }

        public void Update()
        {
            var results = Files
                .SelectMany(file =>
                    {
                        try
                        {
                            return ArgumentFormatter.Format(Arguments == null ? CurrentProgram.Arguments : Arguments.Text, file)
                                .Cast<object>();
                        }
                        catch (Exception ex)
                        {
                            return new object[] {ex};
                        }
                    })
                .Select(r =>
                    {
                        if (r is Exception)
                            return new PreviewItem {Arguments = "ERROR: " + ((Exception) r).Message};

                        return new PreviewItem {Program = CurrentProgram.Filename, Arguments = (string)r};
                    })
                .ToList();

            foreach (var r in results.Where(r => !Results.Contains(r)))
            {
                Results.Add(r);
            }

            var pendingRemoval = Results.Where(r => !results.Contains(r)).ToList();

            foreach (var r in pendingRemoval)
            {
                Results.Remove(r);
            }
        }

        private void Arguments_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Update();
        }
    }

    public class PreviewItem
    {
        public string Program { get; set; }
        public string Arguments { get; set; }
    }
}
