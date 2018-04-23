using System;
using System.IO;
using Topshelf;
using Topshelf.Common.Logging;
using Topshelf.Ninject;

namespace JanoService.Properties
{
    
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {

        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("RunAsLocalSystem")]
        public global::JanoService.AuthenticationType Authentication
        {
            get
            {
                return ((global::JanoService.AuthenticationType)(this["Authentication"]));
            }
        }
    }

}
namespace JanoService
{
    class Program
    {
        static void Main(string[] args)
        {

            // This will ensure that future calls to Directory.GetCurrentDirectory()
            // returns the actual executable directory and not something like C:\Windows\System32 
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // Specify the base name, display name and description for the service, as it is registered in the services control manager.
            // This information is visible through the Windows Service Monitor
            const string serviceName = "JanoService";
            const string displayName = "JanoService";
            const string description = "JanoService add signs into pdfs and it will be sent to TEF";

            HostFactory.Run(x =>
            {


                x.UseCommonLogging();
                x.UseNinject(new IocModule());

                x.Service<Service.WinService>(sc =>
                {

                    sc.ConstructUsingNinject();

                    // the start and stop methods for the service
                    sc.WhenStarted((s, hostControl) => s.Start(hostControl));
                    sc.WhenStopped((s, hostControl) => s.Stop(hostControl));

                    // optional pause/continue methods if used
                    sc.WhenPaused((s, hostControl) => s.Pause(hostControl));
                    sc.WhenContinued((s, hostControl) => s.Continue(hostControl));

                    // optional, when shutdown is supported
                    sc.WhenShutdown((s, hostControl) => s.Shutdown(hostControl));

                });
                //=> Service Identity
                var authenticationType = Properties.Settings.Default.Authentication;
                switch (authenticationType)
                {
                    case AuthenticationType.RunAsPrompt:
                        x.RunAsPrompt();
                        break;
                    case AuthenticationType.RunAsNetworkService:
                        x.RunAsNetworkService();
                        break;
                    case AuthenticationType.RunAsLocalSystem:
                        x.RunAsLocalSystem();
                        break;
                    case AuthenticationType.RunAsLocalService:
                        x.RunAsLocalService();
                        break;
                    case AuthenticationType.RunAs:
                        x.RunAs(Properties.Settings.Default.UserName, Properties.Settings.Default.UserPassword);
                        break;
                }
                //x.RunAsLocalService();
                //=> Service Instalation - These configuration options are used during the service instalation

                //x.StartAutomatically(); // Start the service automatically
                x.StartAutomaticallyDelayed(); // Automatic (Delayed) -- only available on .NET 4.0 or later
                //x.StartManually(); // Start the service manually
                //x.Disabled(); // install the service as disabled

                //=> Service Configuration

                x.EnablePauseAndContinue(); // Specifies that the service supports pause and continue.
                x.EnableShutdown(); //Specifies that the service supports the shutdown service command.

                x.SetDescription(description);
                x.SetDisplayName(displayName);
                x.SetServiceName(serviceName);
            });


        }
    }
}
