using System;
using System.Collections.Generic;
using System.IO;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
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
                .AddJsonFile("appsettings.Local.json", false, false)
                .AddJsonStream(Program.GetRemoteConfig(new List<string> { "http://52.221.19.140:7243/" }));
            _configuration = configBuilder.Build();
            
        }

        [TestCase("32_6", 32, "6")]
        [TestCase("5_vs243lionsgold", 5, "vs243lionsgold")]
        [TestCase("34_SMG_tikiVikings", 34, "SMG_tikiVikings")]
        [TestCase("", 0, "")]
        [TestCase("34SMGtikiVikings", 0, "")]
        public void Test_SplitGameInfo(string gameInfo, int providerId, string gameId)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_configuration);
            services.AddTransient(p => new MySqlClient("", Substitute.For<ILogger<MySqlClient>>()) {DataBase = DataBase.Report});
            services.AddTransient(p => Substitute.For<IMemoryCache>());
            services.AddTransient<CacheHelper>();
            services.AddTransient<CasinoService>();
            var serviceProvider = services.BuildServiceProvider();
            var casinoBll = new CasinoBll(serviceProvider);
            
            var (actualProviderId,actualGameId )= casinoBll.SplitGameInfo(gameInfo);
            
            Assert.AreEqual(providerId, actualProviderId);
            Assert.AreEqual(gameId, actualGameId);
        }
    }
}