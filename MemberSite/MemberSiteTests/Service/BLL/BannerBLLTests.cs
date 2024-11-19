using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using MemberSite.Extensions;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture]
    public class BannerBLLTests
    {
        private IConfiguration _configuration;
        private ServiceCollection services;
        private List<HotGameData> _hotGames;

        [SetUp]
        public void SetUp()
        {
            services = new ServiceCollection();
            services = LoadConfig(services);
            services = MocServerInfoBll(services);
            services = MocPromotionBll(services);

            _hotGames = new List<HotGameData>()
            {
                new HotGameData() { TpId = 10, GameId = "S-FD01", TpGameType = 15, GameName = "5 Fortune Dragons",
                    GameLogo = "{\"HotGamePcUrl\": \"https://banner.0814media.net/img/hotgame/brothers_kingdom.jpg\", \"HotGameMobileUrl\": \"https://banner.0814media.net/img/hotgame/brothers_kingdom.jpg\"}",
                    TpName = "SG", TpLogo = "https://image.0814media.net/SiteSkin/img/logo/sg.png"}
            };
        }

        public async Task Test_GetHotGames()
        {
            services.AddTransient(p => Substitute.For<IWebHostEnvironment>());
            services.AddTransient<TpGameTypeHelper>();
            var serverInfoService = Substitute.For<IBannerService>();
            serverInfoService.GetHotGames(Arg.Any<int>(), Arg.Any<int>())
                .Returns(_hotGames);
            services.AddTransient(p => serverInfoService);
            var serviceProvider = services.BuildServiceProvider();
            var bannerBll = new BannerBLL(serviceProvider);

            var actual = await bannerBll.GetHotGames(1, 2, 1);

            var expected = new List<HotGame>();

            actual.Should().BeEquivalentTo(expected);
        }

        private ServiceCollection LoadConfig(ServiceCollection services)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "LOCAL",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            _configuration = app.Configuration;
            services.AddSingleton(_configuration);
            return services;
        }

        private ServiceCollection MocServerInfoBll(ServiceCollection services)
        {
            services.AddTransient(p => Substitute.For<IServerInfoService>());
            services.AddTransient(p => Substitute.For<IMemoryCache>());
            services.AddTransient(p => Substitute.For<IHttpContextAccessor>());
            services.AddTransient<CacheHelper>();
            services.AddTransient<ServerInfoBll>();
            return services;
        }

        private ServiceCollection MocPromotionBll(ServiceCollection services)
        {
            services.AddTransient<PromotionBll>();
            return services;
        }
    }
}