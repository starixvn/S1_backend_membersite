using MemberSite.Extensions;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Builder;
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
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            _configuration = app.Configuration;
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
            services.AddTransient(p => Substitute.For<IMemoryCache>());
            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient<CacheHelper>();
            services.AddTransient<CasinoService>();
            services.AddDatabaseConnection(_configuration);

            var serviceProvider = services.BuildServiceProvider();
            var casinoBll = new CasinoBll(serviceProvider);

            var (actualProviderId, actualGameId) = casinoBll.SplitGameInfo(gameInfo);

            Assert.AreEqual(providerId, actualProviderId);
            Assert.AreEqual(gameId, actualGameId);
        }
    }
}