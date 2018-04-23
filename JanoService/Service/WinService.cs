using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using Topshelf;

namespace JanoService.Service
{
    class WinServiceController
    {

        public ILog Log { get; private set; }
        readonly Timer _timer;

        public WinServiceController(ILog logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            Log = logger;
            var time = Properties.Settings.Default.TimeScheduled;
            _timer = new Timer(time.TotalMilliseconds) { AutoReset = true };
            _timer.Elapsed += process;
        }

        private void process(object sender, object eventArgs)
        {
            var message = $"It is {DateTime.Now} and all is well";
            Log.Trace($"JanoService - {message}");
            Console.WriteLine(message);
        }

        public bool Start(HostControl hostControl)
        {
            Log.Info($"{nameof(WinServiceController)} Start command received.");
            process(null, null); // Run immediately
            _timer.Start(); // Start scheduler
            return true;
        }

        public bool Stop(HostControl hostControl)
        {

            Log.Trace($"{nameof(WinServiceController)} Stop command received.");
            _timer.Stop();
            return true;

        }

        public bool Pause(HostControl hostControl)
        {

            Log.Trace($"{nameof(WinServiceController)} Pause command received.");

            _timer.Stop();
            return true;

        }

        public bool Continue(HostControl hostControl)
        {

            Log.Trace($"{nameof(Service.WinServiceController)} Continue command received.");
            _timer.Start();
            return true;

        }

        public bool Shutdown(HostControl hostControl)
        {

            Log.Trace($"{nameof(Service.WinServiceController)} Shutdown command received.");
            _timer.Stop();
            return true;

        }

    }
}
