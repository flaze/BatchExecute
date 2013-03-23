using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace BatchExecute
{
    public class ExecuteThread
    {
        private IList<DFile> _files;
        private DProgram _program;

        private MainWindow _main;
        private Thread _thread;
        private string _windowMode;

        public bool IsStopping { get; set; }
        public bool IsRunning { get; set; }

        public delegate void CompleteDelegate();
        public event CompleteDelegate Complete;

        public delegate void StoppedDelegate();
        public event StoppedDelegate Stopped;

        public ExecuteThread(MainWindow main)
        {
            _main = main;
        }

        public void Execute(IList<DFile> files, DProgram program, string windowMode)
        {
            if (IsRunning)
                throw new Exception();

            _files = files;
            _program = program;
            _windowMode = windowMode;

            IsRunning = true;
            _thread = new Thread(new ThreadStart(Work));
            _thread.Start();
        }

        public void Stop()
        {
            IsStopping = true;
        }

        private void Work()
        {
            for (int i = 0; i < _files.Count; i++)
            {
                DFile file = _files[i];
                string arguments = _program.Arguments.Inject(file);

                Debug.WriteLine("EXECUTE: " + _program.Filename + " " + arguments);

                ProcessStartInfo startInfo = new ProcessStartInfo(_program.Filename, arguments);

                if (_windowMode == "Hidden")
                {
                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else if (_windowMode == "Minimized")
                {
                    startInfo.WindowStyle = ProcessWindowStyle.Minimized;
                }

                Process p = Process.Start(startInfo);

                p.WaitForExit();

                _main.UpdateState(i, "Done");

                if (IsStopping) break;
            }

            if (IsStopping)
            {
                IsStopping = false;
                if (Stopped != null)
                    Stopped();
            }

            IsRunning = false;
            if (Complete != null)
                Complete();
        }
    }
}
