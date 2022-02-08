using NUnit.Framework;
using MemberSite.Service.BLL;
using System;
using System.Collections.Generic;
using System.Text;
using MemberSiteTests.Stubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using NSubstitute;
using MemberSite.Service.Library;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture()]
    public class AuthorizeBLLTests
    {
        private readonly IConfiguration _configuration;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly Tools _tools;
        public AuthorizeBLLTests()
        {
            var services = new ServiceCollection();

            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .AddJsonFile("appsettings.Local.json", false, false)
                .AddJsonStream(Program.GetRemoteConfig(new List<string> { "http://52.221.19.140:7243/" }));
            _configuration = configBuilder.Build();
            services.AddSingleton(_configuration);
            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());

            var startup = new StubStartUp(_configuration, new HostingEnvironment()
            {
                EnvironmentName = Environments.Development,
            });

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _tools = provider.GetRequiredService<Tools>();
        }

        [TestCase(null, ExpectedResult = false)]
        [TestCase("eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTUxMiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoicWF0ZXN0MDEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjciLCJUb2tlbiI6IjJZYmllZW16bWtlRTJLMnMzeld3UEEiLCJMYW5nSWQiOiJlbiIsImp0aSI6Ijc2ZTBmNDg3LTMzMmMtNGQzZi1hNWFiLTA1YjRiODE3MTlhNiIsImV4cCI6MTk0NzI5MTY3MCwiaXNzIjoiTW9iaWxlQXBpU2VydmVyIn0.afQqwG839fKzy21vFjwrGUZ8Hp_iEKUH7YmXfDXLmNcTy1FKHxMZg_0ZHMnV2itwMp8xPJCWjDadH0yNtgLpIw", ExpectedResult = true)]
        [TestCase("aeyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTUxMiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoicWF0ZXN0MDEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjciLCJUb2tlbiI6IjJZYmllZW16bWtlRTJLMnMzeld3UEEiLCJMYW5nSWQiOiJlbiIsImp0aSI6Ijc2ZTBmNDg3LTMzMmMtNGQzZi1hNWFiLTA1YjRiODE3MTlhNiIsImV4cCI6MTk0NzI5MTY3MCwiaXNzIjoiTW9iaWxlQXBpU2VydmVyIn0.afQqwG839fKzy21vFjwrGUZ8Hp_iEKUH7YmXfDXLmNcTy1FKHxMZg_0ZHMnV2itwMp8xPJCWjDadH0yNtgLpIw", ExpectedResult = false)]
        public bool TryValidateJwtTokenTest(string jwtToken) => _authorizeBll.TryValidateJwtToken(jwtToken,out var _);

        [Test]
        public void JwtTokenGetAuthDataTest()
        {
            var token = Guid.NewGuid().ToString();
            var claims = new[]
            {
                    new Claim(ClaimTypes.Name,"test"),
                    new Claim(ClaimTypes.NameIdentifier,"1234") ,
                    new Claim("Token", token ),
                    new Claim("LangId","en"),
                    new Claim(JwtRegisteredClaimNames.Jti, token )
            };
            var jwtToken = _tools.GenerateJwtTokens(claims);
            var actual = _authorizeBll.TryValidateJwtToken(jwtToken, out var authData);
            Assert.IsTrue(actual);
            Assert.AreEqual("test", authData.UserName);
            Assert.AreEqual(1234, authData.CustId);
            Assert.AreEqual(token, authData.Token);
            Assert.AreEqual("en", authData.LangId);
        }
    }
}