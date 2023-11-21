using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using MemberSite.Extensions;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture]
    public class ServerInfoBLLTests
    {
        private IConfiguration _configuration;
        private List<MenuOfGameData> _menuOfGames;
        [SetUp]
        public void SetUp()
        {
            _menuOfGames = new List<MenuOfGameData>()
            {
                new MenuOfGameData() {MenuId = 1, TpId = 1, GameId = "1", TpGameType = 1, Sort = 0},
                new MenuOfGameData() {MenuId = 1, TpId = 1, GameId = "2", TpGameType = 1, Sort = 2},
                new MenuOfGameData() {MenuId = 1, TpId = 1, GameId = "5", TpGameType = 1, Sort = 3},
                new MenuOfGameData() {MenuId = 2, TpId = 1, GameId = "43", TpGameType = 1, Sort = 0},
                new MenuOfGameData() {MenuId = 1, TpId = 1, GameId = "50", TpGameType = 1, Sort = 4},
                new MenuOfGameData() {MenuId = 1, TpId = 1, GameId = "997", TpGameType = 1, Sort = 1}
            };
        }

        [Test]
        public async Task Test_Get_MenuOfGames()
        {
            var services = new ServiceCollection();
            services = LoadConfig(services);
            services.AddTransient(p => Substitute.For<IMemoryCache>());
            services.AddTransient(p => Substitute.For<IHttpContextAccessor>());
            services.AddTransient<CacheHelper>();
            var serverInfoService = Substitute.For<IServerInfoService>();
            serverInfoService.GetMenuOfGames(Arg.Any<int>(), Arg.Any<string>())
                .Returns(_menuOfGames);
            services.AddTransient(p => serverInfoService);
            var serviceProvider = services.BuildServiceProvider();
            var serverInfoBll = new ServerInfoBll(serviceProvider);

            var actual = await serverInfoBll.GetMenuOfGames(1, "th");

            var expect = new List<MenuOfGame>()
            {
                new MenuOfGame()
                {
                    MenuId = 1, MenuItems = new List<MenuItem>()
                    {
                        new MenuItem() {TpId = 1, GameId = "1", TpGameType = 1, Sort = 0},
                        new MenuItem() {TpId = 1, GameId = "997", TpGameType = 1, Sort = 1},
                        new MenuItem() {TpId = 1, GameId = "2", TpGameType = 1, Sort = 2},
                        new MenuItem() {TpId = 1, GameId = "5", TpGameType = 1, Sort = 3},
                        new MenuItem() {TpId = 1, GameId = "50", TpGameType = 1, Sort = 4},
                    }
                },
                new MenuOfGame()
                {
                    MenuId = 2, MenuItems = new List<MenuItem>()
                    {
                        new MenuItem() {TpId = 1, GameId = "43", TpGameType = 1, Sort = 0},
                    }
                }
            };
            actual.Should().BeEquivalentTo(expect);
        }

        private ServiceCollection LoadConfig(ServiceCollection services)
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
            services.AddSingleton(_configuration);
            return services;
        }
    }
}