using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace BatchExecute
{
    public class DProgram : INotifyPropertyChanged
    {
        private string _name;
        private string _filename;
        private string _arguments;
        private bool _isRunning;

        public string Name
        {
            get { return _name; }
            set
            { 
                _name = value;
                FirePropertyChanged("Name");
                FirePropertyChanged("Execute_Text");
                FirePropertyChanged("Execute_Enabled");
            }
        }

        public string Filename
        {
            get { return _filename; }
            set
            {
                _filename = value;
                FirePropertyChanged("Filename");
                FirePropertyChanged("Execute_Enabled");
            }
        }

        public string Arguments
        {
            get { return _arguments; }
            set
            {
                _arguments = value;
                FirePropertyChanged("Arguments");
                FirePropertyChanged("Execute_Enabled");
            }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                FirePropertyChanged("IsRunning");
                FirePropertyChanged("Execute_Enabled");
            }
        }

        public string Execute_Text
        {
            get
            {
                return "Execute " + Name;
            }
        }

        public bool Execute_Enabled
        {
            get
            {
                return !IsRunning && !string.IsNullOrEmpty(Filename) && !string.IsNullOrEmpty(Arguments);
            }
        }

        public DProgram()
        {
            Name = "<NO NAME>";
            Filename = "";
            Arguments = "";
        }

        public DProgram(string name, string filename, string arguments)
        {
            Name = name;
            Filename = filename;
            Arguments = arguments;
        }

        public override string ToString()
        {
            return Name != null ? Name : "";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
