//using MemberSite.Service.BLL;
//using MemberSite.Service.Library;
//using MemberSiteTests.Stubs;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting.Internal;
//using Microsoft.Extensions.Hosting;
//using MemberSite.Extensions;
//using Microsoft.Extensions.Configuration;
//using NSubstitute;
//using Microsoft.Extensions.Logging;
//using NUnit.Framework;
//using System;

//namespace MemberSite.Service.BLL.Tests;

//internal class AuthBLLTests
//{
//    private readonly IConfiguration _configuration;
//    private readonly CryptoHelper _cryptoHelper;
//    private readonly AuthBLL _authBll;
//    private readonly string _publicApiAuthToken;

//    public AuthBLLTests()
//    {
//        var services = new ServiceCollection();

//        var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
//        {
//            EnvironmentName = "Local",
//        });
//        builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());
//        var app = builder.Build();
//        _configuration = app.Configuration;

//        services.AddSingleton(_configuration);
//        services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>()); 
//        services.AddSingleton(p => Substitute.For<ILogger<AuthBLL>>());

        var startup = new StubStartUp(_configuration);

//        startup.ConfigureServices(services);
//        var provider = services.BuildServiceProvider();

//        _authBll = provider.GetRequiredService<AuthBLL>();
//        _cryptoHelper = provider.GetRequiredService<CryptoHelper>();
//        _publicApiAuthToken = provider.GetRequiredService<IConfiguration>().GetValue<string>("PublicApiAuthToken");
//    }

//    /// <summary>
//    /// Api/Site/SignIn 為例
//    /// </summary>
//    private string requestPlainText = "{\"UserName\":\"EttaTestMYR\"," +
//            "\"Userpwd\":\"1qaz@WSX\"," +
//            "\"isPersistent\":true," +
//            "\"url\":\"https://www.n88dev.com\"," +
//            "\"platform\":\"Win32\"," +
//            "\"userAgent\":\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36\"," +
//            "\"screen\":\"1280:720:24\",";
//    private string requestTimestamp = "\"timestamp\":" + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + "}";

    private string fromUrl = "test";


    /// <summary>
    /// 無效content
    /// </summary>
    /// <param name="content">request 內文</param>
    /// <param name="expectedResult">預期結果</param>
    [TestCase(null, false)]
    [TestCase("123456789", false)]
    public void RequestAuthTestWithInvaildContent(string content, bool expectedResult)
    {
        var result = _authBll.RequestAuth(fromUrl, content);
        Assert.AreEqual(result, expectedResult);
    }

    /// <summary>
    /// 正確的request資訊
    /// </summary>
    [Test]
    public void RequestAuthTestWithVaildContent()
    {
        var content = requestPlainText
            + "\"token\":\""
            + _cryptoHelper.SHA256(requestPlainText
            + "\"token\":\"" + _publicApiAuthToken + "\","
            + requestTimestamp)
            + "\","
            + requestTimestamp;
        var result = _authBll.RequestAuth(fromUrl, content);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// request content沒有Timestamp
    /// </summary>
    [Test]
    public void RequestAuthTestWithoutTimestamp()
    {
        var content = requestPlainText
            + "\"token\":\""
            + _cryptoHelper.SHA256(requestPlainText
            + "\"token\":\"" + _publicApiAuthToken + "\","
            + requestTimestamp)
            + "\"}";
        var result = _authBll.RequestAuth(fromUrl, content);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// 無效的Timestamp(時間沒有在30秒之內)
    /// </summary>
    [Test]
    public void RequestAuthTestWithInVaildTimestampNumber()
    {
        var content = requestPlainText
            + "\"token\":\""
            + _cryptoHelper.SHA256(requestPlainText
            + "\"token\":\"" + _publicApiAuthToken + "\","
            + requestTimestamp)
            + "\","
            + "\"timestamp\":" + 1640995200 + "}"; //2022-01-01 00:00:00 GMT+8
        var result = _authBll.RequestAuth(fromUrl, content);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// 無效的Timestamp(字串)
    /// </summary>
    [Test]
    public void RequestAuthTestWithInVaildTimestampString()
    {
        var content = requestPlainText
            + "\"token\":\""
            + _cryptoHelper.SHA256(requestPlainText
            + "\"token\":\"" + _publicApiAuthToken + "\","
            + requestTimestamp)
            + "\","
            + "\"timestamp\":\"qwertyuiop\"}"; //2022-01-01 00:00:00 GMT+8
        var result = _authBll.RequestAuth(fromUrl, content);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// request content沒有Token
    /// </summary>
    [Test]
    public void RequestAuthTestWithoutToken()
    {
        var content = requestPlainText + requestTimestamp;
        var result = _authBll.RequestAuth(fromUrl, content);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// 無效的Token
    /// </summary>
    [Test]
    public void RequestAuthTestWithInVaildToken()
    {
        var content = requestPlainText +
            "\"token\":\"1qaz2wsx3edc4rfv\","
            + requestTimestamp;
        var result = _authBll.RequestAuth(fromUrl, content);
        Assert.IsFalse(result);
    }
}
