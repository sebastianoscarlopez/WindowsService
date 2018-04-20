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
    class WinService
    {

        public ILog Log { get; private set; }
        readonly Timer _timer;
        public WinService(ILog logger)
        {
            /*
            // IocModule.cs needs to be updated in case new paramteres are added to this constructor

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            Log = logger;
            _timer = new Timer(1000) { AutoReset = true };
            _timer.Elapsed += (sender, eventArgs) => Console.WriteLine("It is {0} and all is well", DateTime.Now);
            */
        }

        public bool Start(HostControl hostControl)
        {

            Log.Info($"{nameof(Service.WinService)} Start command received.");
          //  _timer.Start();
            return true;

        }

        public bool Stop(HostControl hostControl)
        {

            Log.Trace($"{nameof(Service.WinService)} Stop command received.");
            //_timer.Stop();
            return true;

        }

        public bool Pause(HostControl hostControl)
        {

            Log.Trace($"{nameof(Service.WinService)} Pause command received.");

            //_timer.Stop();
            return true;

        }

        public bool Continue(HostControl hostControl)
        {

            Log.Trace($"{nameof(Service.WinService)} Continue command received.");
            //_timer.Start();
            return true;

        }

        public bool Shutdown(HostControl hostControl)
        {

            Log.Trace($"{nameof(Service.WinService)} Shutdown command received.");
            //_timer.Stop();
            return true;

        }

    }
}
