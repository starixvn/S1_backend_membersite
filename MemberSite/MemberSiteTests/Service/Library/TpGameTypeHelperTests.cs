using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using MemberSite.Service.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using MemberSiteTests.Stubs;
using NSubstitute;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Diagnostics;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Builder;
using MemberSite.Extensions;

namespace MemberSite.Service.Library.Tests
{
    [TestFixture()]
    public class TpGameTypeHelperTests
    {
        private IConfiguration _configuration;
        private TpGameTypeHelper _tpGameTypeHelper;
        private MySqlClient _dbClient;

        public TpGameTypeHelperTests()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });
            builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());
            var app = builder.Build();
            _configuration = app.Configuration;
            services.AddSingleton(_configuration);

            var logger = Substitute.For<ILogger<MySqlClient>>();
            services.AddTransient(p => logger);

            var startup = new StubStartUp(_configuration, new HostingEnvironment()
            {
                EnvironmentName = Environments.Development,
            });

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _tpGameTypeHelper = provider.GetRequiredService<TpGameTypeHelper>();
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        [TestCase(1, ExpectedResult = 0)]
        [TestCase(2, ExpectedResult = -1)]
        [TestCase(3, ExpectedResult = 0)]
        [TestCase(4, ExpectedResult = 0)]
        [TestCase(5, ExpectedResult = 0)]
        [TestCase(6, ExpectedResult = -1)]
        [TestCase(7, ExpectedResult = 0)]
        public int ActionMode_Test(int actionMode)
            => _tpGameTypeHelper.GetTpGameType(new BannerV3() { ActionMode = actionMode, TpId = "0" }, 2);


        [TestCase(1, ExpectedResult = 0)]
        [TestCase(2, ExpectedResult = 1)]
        [TestCase(3, ExpectedResult = 2)]
        [TestCase(4, ExpectedResult = 3)]
        [TestCase(5, ExpectedResult = 4)]
        [TestCase(6, ExpectedResult = 6)]
        [TestCase(7, ExpectedResult = 0)]
        public int GetTpCategoryId_Test(int bannerType)
            => _tpGameTypeHelper.GetTpCategoryIdByBannerType(bannerType);


        [TestCase("MG RNG", ExpectedResult = 57)]
        [TestCase("MG L.Casino", ExpectedResult = 58)]
        public int BannerName_Test(string typeName)
            => _tpGameTypeHelper.GetTpGameType(new BannerV3()
            {
                ActionMode = 2,
                BannerType = 1,
                BannerName = $"testBanner-{typeName}",
                TpId = "34"
            }, 2);


        [TestCase("1", ExpectedResult = 69)]
        [TestCase("2", ExpectedResult = 69)]
        [TestCase("161", ExpectedResult = 70)]
        [TestCase("164", ExpectedResult = 70)]
        [TestCase("168", ExpectedResult = 70)]
        [TestCase("180", ExpectedResult = 71)]
        [TestCase("181", ExpectedResult = 71)]
        [TestCase("182", ExpectedResult = 71)]
        [TestCase("183", ExpectedResult = 71)]
        [TestCase("184", ExpectedResult = 71)]
        [TestCase("185", ExpectedResult = 71)]
        [TestCase("186", ExpectedResult = 71)]
        [TestCase("208", ExpectedResult = 0)]
        [TestCase("202", ExpectedResult = 73)]
        [TestCase("220", ExpectedResult = 74)]
        [TestCase("223", ExpectedResult = 0)]
        [TestCase("165", ExpectedResult = 0)]
        [TestCase("245", ExpectedResult = 0)]
        [TestCase("222", ExpectedResult = 75)]
        [TestCase("199", ExpectedResult = 72)]
        [TestCase("200", ExpectedResult = 0)]
        [TestCase("212", ExpectedResult = 0)]
        [TestCase("56", ExpectedResult = 69)]
        public int Saba_Test(string gameId)
            => _tpGameTypeHelper.GetTpGameType(new BannerV3()
            {
                ActionMode = 2,
                BannerType = 1,
                BannerName = $"testBanner",
                TpId = "41",
                GameId = gameId
            }, 2);

        [TestCase("sports", "3", ExpectedResult = 69)]
       
        public int UrlRouter_Test(string urlRouter, string gameId)
            => _tpGameTypeHelper.GetTpGameType(new BannerV3()
            {
                BannerName = $"testBanner",
                ActionMode = 2,
                BannerType = 1,
                UrlRouter = urlRouter,
                TpId = "41",
                GameId = gameId
            }, 2);

        [TestCase("2", ExpectedResult = 9)]
        [TestCase("3", ExpectedResult = 8)]
        [TestCase("4", ExpectedResult = 10)]
        public int TpId_Test(string TpId)
            => _tpGameTypeHelper.GetTpGameType(new BannerV3()
            {
                BannerName = $"testBanner",
                ActionMode = 2,
                BannerType = 1,
                TpId = TpId,
            }, 2);


        [Ignore("開發測試用")]
        public void All_Test()
        {
            var bannerList = _dbClient.Query<BannerV3>("SELECT * FROM Banner");
            foreach (var banner in bannerList)
            {
                var tpGameType = _tpGameTypeHelper.GetTpGameType(banner, 2);
                Trace.WriteLine(tpGameType);
            }
        }
    }
}