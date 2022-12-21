using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSiteTests.Stubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting;
using MemberSite.Extensions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Security.Claims;
using System;
using System.Text;
using System.Security.Cryptography;

namespace MemberSiteTests.Service.BLL;

internal class AuthBLLTests
{
    private readonly IConfiguration _configuration;
    private readonly AuthBLL _authBll;

    public AuthBLLTests()
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
        services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());

        var startup = new StubStartUp(_configuration, new HostingEnvironment()
        {
            EnvironmentName = Environments.Development,
        });

        startup.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        _authBll = provider.GetRequiredService<AuthBLL>();
    }

    private static string requestPlainText = "{\"UserName\":\"EttaTestMYR\"," +
            "\"Userpwd\":\"1qaz@WSX\"," +
            "\"isPersistent\":true," +
            "\"url\":\"https://www.n88dev.com\"," +
            "\"platform\":\"Win32\"," +
            "\"userAgent\":\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36\"," +
            "\"screen\":\"1280:720:24\",";
    private static string requestPlainToken = "\"Token\":\"1qaz2wsx\",";
    private static string requestTimestamp = "\"Timestamp\":" + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + "}";
    private static string request = requestPlainText + "\"Token\":\"" + SHA256(requestPlainText + requestPlainToken + requestTimestamp) + "\"," + requestTimestamp;

    static object[] TestCases =
   {
        new object[] { null, false },
        //new object[] { "123", false },
        new object[] { request, true }
    };
    [TestCaseSource(nameof(TestCases))]
    public void RequestAuthTest(string content, bool expectedResult)
    {
        var result = _authBll.RequestAuth(content);
        Assert.AreEqual(result, expectedResult);
    }

    private static string SHA256(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
        return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
    }
}
