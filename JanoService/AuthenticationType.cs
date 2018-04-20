using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JanoService
{
    [Serializable]
    public enum AuthenticationType
    {
        RunAsPrompt, // when service is installed, the installer will prompt for a username and password
        RunAsNetworkService, // runs as the NETWORK_SERVICE built-in account
        RunAsLocalSystem, // run as the local system account
        RunAsLocalService, // run as the local service account
        RunAs // predefined user, It will used UserName and UserPassword key
    }
}
