using MemberSite;

using Microsoft.Extensions.Configuration;

namespace MemberSiteTests.Stubs
{
    public class StubStartUp : Startup
    {
        public StubStartUp(IConfiguration configuration) : base(configuration) { }
    }
}
