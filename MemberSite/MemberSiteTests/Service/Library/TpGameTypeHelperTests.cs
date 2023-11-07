using System.Diagnostics;
using System.Linq;

using MemberSite.Extensions;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using MemberSiteTests.Stubs;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using NUnit.Framework;

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

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            _configuration = app.Configuration;
            services.AddSingleton(_configuration);

            var logger = Substitute.For<ILogger<MySqlClient>>();
            services.AddTransient(p => logger);

            var startup = new StubStartUp(_configuration);

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


        [TestCase("1", ExpectedResult = 1)]
        [TestCase("2", ExpectedResult = 1)]
        [TestCase("198", ExpectedResult = 1)]
        [TestCase("161", ExpectedResult = 2)]
        [TestCase("164", ExpectedResult = 2)]
        [TestCase("168", ExpectedResult = 2)]
        [TestCase("180", ExpectedResult = 3)]
        [TestCase("181", ExpectedResult = 3)]
        [TestCase("182", ExpectedResult = 3)]
        [TestCase("183", ExpectedResult = 3)]
        [TestCase("184", ExpectedResult = 3)]
        [TestCase("185", ExpectedResult = 3)]
        [TestCase("186", ExpectedResult = 3)]
        [TestCase("208", ExpectedResult = 4)]
        [TestCase("202", ExpectedResult = 5)]
        [TestCase("220", ExpectedResult = 19)]
        [TestCase("223", ExpectedResult = 20)]
        [TestCase("165", ExpectedResult = 21)]
        [TestCase("245", ExpectedResult = 22)]
        [TestCase("222", ExpectedResult = 23)]
        [TestCase("199", ExpectedResult = 24)]
        [TestCase("200", ExpectedResult = 25)]
        [TestCase("212", ExpectedResult = 26)]
        [TestCase("56", ExpectedResult = 38)]
        [TestCase("171", ExpectedResult = 59)]
        [TestCase("2001", ExpectedResult = 1)]
        [TestCase("2002", ExpectedResult = 1)]
        [TestCase("2003", ExpectedResult = 1)]
        public int Saba_Test(string gameId)
            => _tpGameTypeHelper.GetTpGameType(new BannerV3()
            {
                ActionMode = 2,
                BannerType = 1,
                BannerName = $"testBanner",
                TpId = "1",
                GameId = gameId
            }, 2);

        [TestCase("sports", "3", ExpectedResult = 1)]
        [TestCase("casino", "245", ExpectedResult = 22)]
        public int UrlRouter_Test(string urlRouter, string gameId)
            => _tpGameTypeHelper.GetTpGameType(new BannerV3()
            {
                BannerName = $"testBanner",
                ActionMode = 2,
                BannerType = 1,
                UrlRouter = urlRouter,
                TpId = "1",
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