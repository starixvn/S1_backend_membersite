using System.Threading.Tasks;

using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Model;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture]
    public class GeoIpBLLTest
    {
        [Test]
        public async Task Test_CheckIp_And_Return_Succeed()
        {
            var serviceCollection = new ServiceCollection();
            var geoIpApi = Substitute.For<IGeoIpApi>();
            geoIpApi.CheckIp(Arg.Any<string>()).Returns(a =>
                new IpCountry() { Code = "TW", Forbidden = false, Message = "Succeed" });
            serviceCollection.AddTransient(p => geoIpApi);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var geoIpBll = new GeoIpBLL(serviceProvider);

            var actual = await geoIpBll.CheckIp("125.227.148.102");

            Assert.AreEqual(0, actual.ErrorCode);
        }

        [Test]
        public async Task Test_CheckIp_And_Return_Failed()
        {
            var serviceCollection = new ServiceCollection();
            var geoIpApi = Substitute.For<IGeoIpApi>();
            geoIpApi.CheckIp(Arg.Any<string>()).Returns(a =>
                new IpCountry() { Message = "IP format error" });
            serviceCollection.AddTransient(p => geoIpApi);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var geoIpBll = new GeoIpBLL(serviceProvider);

            var actual = await geoIpBll.CheckIp("125.227.148.102");

            Assert.AreEqual(9000, actual.ErrorCode);
        }
    }
}