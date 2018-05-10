using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers;
using Topshelf;

namespace JanoService.Service
{
    class WinServiceController
    {
        private readonly IObservable<long> _time;
        private IDisposable timeDispose;
        private readonly Process process;

        public ILog Log { get; private set; }

        public WinServiceController(ILog logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            process = new Process(logger);

            Log = logger;
            _time = Observable.Interval(
                Properties.Settings.Default.TimeScheduled
            );
        }

        void start()
        {
            try
            {
                if (timeDispose == null)
                {
                    process.run(); // It will be started without delay
                    timeDispose = _time
                        .ObserveOn(CurrentThreadScheduler.Instance)
                        .SubscribeOn(NewThreadScheduler.Default)
                        .Subscribe(observer => // Time elapsed
                        {
                            process.run();
                        }, (ex) => // Error
                        {
                            Log.Error($"Error: {ex.Message} \r\n\tSource:{ex.Source} \r\n\tStackTrace:{ex.StackTrace}");
                            stop();
                            Thread.Sleep(1000);
                            start();
                        });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ERROR was unexpected: {ex.Message} \r\n\tSource:{ex.Source} \r\n\tStackTrace:{ex.StackTrace}");
                start();
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
