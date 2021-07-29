using System.IO;
using System.Linq;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace MemberSite.Service.Model.Tests
{
    [TestFixture]
    public class CasinoBLLTests
    {
        private IConfiguration _configuration;
        
        [SetUp]
        public void SetUp()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .AddJsonFile("appsettings.Local.json", false, false);
            _configuration = configBuilder.Build();
            
        }

        [TestCase("32_6", 32, "6")]
        [TestCase("5_vs243lionsgold", 5, "vs243lionsgold")]
        [TestCase("34_SMG_tikiVikings", 34, "SMG_tikiVikings")]
        [TestCase("", 0, "")]
        [TestCase("34SMGtikiVikings", 0, "")]
        public void Test_SplitGameInfo(string gameInfo, int providerId, string gameId)
        {
            
            var memoryCache = Substitute.For<IMemoryCache>();
            var casinoService = Substitute.For<CasinoService>(_configuration);
            var casinoBll = new CasinoBLL(memoryCache, casinoService);


            var (actualProviderId,actualGameId )= casinoBll.SplitGameInfo(gameInfo);
            
            Assert.AreEqual(providerId, actualProviderId);
            Assert.AreEqual(gameId, actualGameId);
        }
    }
}