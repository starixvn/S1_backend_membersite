using System;
using System.IO;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace MemberSite.Service.Model.Tests
{
    [TestFixture]
    public class GeoIpBLLTest
    {
        [Test]
        public async Task Test_CheckIp_And_Return_Succeed()
        {
            var serviceCollection = new ServiceCollection();
            var geoIpApi = Substitute.For<IGeoIpApiBLL>();
            geoIpApi.CheckIp(Arg.Any<string>()).Returns(a => 
                new IpCountry() { Code = "TW", Forbidden = false, Message = "Succeed"});
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
            var geoIpApi = Substitute.For<IGeoIpApiBLL>();
            geoIpApi.CheckIp(Arg.Any<string>()).Returns(a => 
                new IpCountry() { Message = "IP format error"});
            serviceCollection.AddTransient(p => geoIpApi);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var geoIpBll = new GeoIpBLL(serviceProvider);

            var actual = await geoIpBll.CheckIp("125.227.148.102");

            Assert.AreEqual(9000, actual.ErrorCode);
        }
    }
}