﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace MTGAHelper.Tracker.WPF.Business.Monitoring
{
    public class ProcessMonitor
    {
        private const string PROCESS_NAME = "MTGA";

        public bool IsRunning { get; private set; }

        public event Action<object, bool> OnProcessMonitorStatusChanged;

        public ProcessMonitor()
        {
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            await ContinuallyCheckForProcess(cancellationToken);
        }

        private Task ContinuallyCheckForProcess(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new TaskCanceledException((Task) null);

                        bool processFound = System.Diagnostics.Process.GetProcessesByName(PROCESS_NAME).Length > 0;
                        bool stateChanged = processFound != IsRunning;
                        if (stateChanged)
                        {
                            IsRunning = processFound;
                            OnProcessMonitorStatusChanged?.Invoke(this, IsRunning);
                        }
                    }
                    catch (Exception)
                    {
                        //Log.Fatal(ex, "Unexpected error:");
                    }

                    Thread.Sleep(1000);
                }
            }, cancellationToken);

            return task;
        }
    }
}
