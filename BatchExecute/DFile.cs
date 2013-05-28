using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace BatchExecute
{
    public class DFile : INotifyPropertyChanged
    {
        private string _state;

        public FileInfo FileInfo { get; set; }
        
        public virtual string Path
        {
            get
            {
                if (FileInfo != null)
                    return FileInfo.FullName;
                return "";
            }
            set { throw new NotSupportedException(); }
        }

        public virtual string FullName
        {
            get
            {
                if (FileInfo != null)
                    return FileInfo.FullName;
                return "";
            }
            set { throw new NotSupportedException(); }
        }

        public virtual string DirectoryName
        {
            get
            {
                if (FileInfo != null)
                    return FileInfo.DirectoryName;
                return "";
            }
            set { throw new NotSupportedException(); }
        }

        public virtual string Name
        {
            get
            {
                if(FileInfo != null)
                    return FileInfo.Name.Substring(0, FileInfo.Name.Length - FileInfo.Extension.Length);
                return "";
            }
            set { throw new NotSupportedException(); }
        }

        public virtual string Extension
        {
            get
            {
                if (FileInfo != null)
                    return FileInfo.Extension.Substring(1);
                return "";
            }
            set { throw new NotSupportedException(); }
        }

        public string State
        {
            get { return _state; }
            set
            {
                _state = value;
                FirePropertyChanged("State");
            }
        }

        public DFile()
            : this(null)
        {
        }

        public DFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            State = "Pending";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
