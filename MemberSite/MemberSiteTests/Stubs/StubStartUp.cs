using MemberSite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MemberSiteTests.Stubs
{
    public class StubStartUp : Startup
    {
        public StubStartUp(IConfiguration configuration, IHostEnvironment environment) : base(configuration) { }
    }
}
