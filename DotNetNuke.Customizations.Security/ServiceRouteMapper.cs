#region Usings

using System.Net;
using DotNetNuke.Web.Api;

#endregion

namespace DotNetNuke.Customizations.Security
{
    public class ServiceRouteMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            // Enable TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // .NET 4.5
        }
    }
}
