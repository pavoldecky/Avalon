using Avalon.Shared.Configuration.Identity;
using Avalon.STS.Identity.Configuration.Interfaces;

namespace Avalon.STS.Identity.Configuration
{
    public class RootConfiguration : IRootConfiguration
    {      
        public AdminConfiguration AdminConfiguration { get; } = new AdminConfiguration();
        public RegisterConfiguration RegisterConfiguration { get; } = new RegisterConfiguration();
    }
}





