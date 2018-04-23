using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using Topshelf;

namespace JanoService.Service
{
    class WinServiceController
    {
        private readonly IObservable<long> _time;
        private IDisposable timeDispose;

        public ILog Log { get; private set; }

        public WinServiceController(ILog logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            
            Log = logger;
            _time = Observable.Interval(
                Properties.Settings.Default.TimeScheduled,
                NewThreadScheduler.Default
            );
        }

        private void process()
        {
            PendientesTramite.GetInstance()
                .updatePendientes();
            var message = $"It is {DateTime.Now} and all is well";
            Log.Info($"JanoService - {message}");
            Console.WriteLine(message);
        }

        void start()
        {
            if (timeDispose == null)
            {
                timeDispose = _time.Subscribe(observer =>
                {
                    process();
                });
            }
        }
        void stop()
        {
            timeDispose?.Dispose();
            timeDispose = null;
        }

        public bool Start(HostControl hostControl)
        {
            Log.Info($"{nameof(WinServiceController)} Start command received.");
            start();
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Log.Trace($"{nameof(WinServiceController)} Stop command received.");
            stop();
            return true;
        }

        public bool Pause(HostControl hostControl)
        {

            Log.Trace($"{nameof(WinServiceController)} Pause command received.");
            stop();
            return true;

        }

        public bool Continue(HostControl hostControl)
        {
            Log.Trace($"{nameof(Service.WinServiceController)} Continue command received.");
            start();
            return true;
        }

        public bool Shutdown(HostControl hostControl)
        {
            Log.Trace($"{nameof(Service.WinServiceController)} Shutdown command received.");
            stop();
            return true;
        }

    }
}
