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

        private readonly MainWindow _main;
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
            _thread = new Thread(Work);
            _thread.Start();
        }

        public void Stop()
        {
            IsStopping = true;
        }

        private void Work()
        {
            for (var i = 0; i < _files.Count; i++)
            {
                var file = _files[i];
                var stepArguments = ArgumentFormatter.Format(_program.Arguments, file);

                foreach (var arguments in stepArguments)
                {
                    Debug.WriteLine("EXECUTE: " + _program.Filename + " " + arguments);

                    var p = StartProgram(arguments);
                    p.WaitForExit();
                }

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

        private Process StartProgram(string arguments)
        {
            var startInfo = new ProcessStartInfo(_program.Filename, arguments);

            if (_windowMode == "Hidden")
            {
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }
            else if (_windowMode == "Minimized")
            {
                startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            }

            return Process.Start(startInfo);
        }
    }
}
