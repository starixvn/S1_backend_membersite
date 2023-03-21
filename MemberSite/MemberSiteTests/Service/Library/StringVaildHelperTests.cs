using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using MemberSiteTests.Stubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MemberSite.Extensions;
using Microsoft.Extensions.Configuration;
using MemberSite.Service.Model;
using NUnit.Framework;

namespace MemberSite.Service.Library.Tests;

internal class StringVaildHelperTests
{
    private IConfiguration _configuration;
    private StringVaildHelper _stringVaildHelper;

    public StringVaildHelperTests()
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

        var startup = new StubStartUp(_configuration);

        startup.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        _stringVaildHelper = provider.GetRequiredService<StringVaildHelper>();
    }

    [TestCase("qazWSXedc", ExpectedResult = true)]              //大小寫混合
    [TestCase("qazwsxedc", ExpectedResult = true)]              //全小寫
    [TestCase("QAZWSXEDC", ExpectedResult = true)]              //全大寫
    [TestCase("QAZWSXEDC awgsddfdf", ExpectedResult = true)]    //大小寫混合，可以有空白
    [TestCase("qawsedrf !@#$%^", ExpectedResult = false)]        //不可有無效特殊符號
    [TestCase("qawsedrf12345", ExpectedResult = false)]         //不可以有數字
    [TestCase("qawsedrf12345./!@#$%^", ExpectedResult = false)]  //不可以有數字&無效特殊符號
    [TestCase("qawsedrf./", ExpectedResult = true)]             //混有效特殊程式
    [TestCase("qawsedrf./12345", ExpectedResult = false)]       //混有效特殊程式，不可以有數字
    [TestCase("qawsedrf\"", ExpectedResult = false)]       //混有效特殊程式，不可以有數字
    [TestCase("supermainian A/L mamian", ExpectedResult = true)]       //from marketing test case
    [TestCase("nur asyikien bt. abdullah", ExpectedResult = true)]       //from marketing test case
    [TestCase("Cheong Siew Shan", ExpectedResult = true)]       //from marketing test case
    [TestCase("Halimah @ husan", ExpectedResult = false)]       //from marketing test case
    [TestCase("～ ĎĔĂŘ ～", ExpectedResult = false)]       //from marketing test case
    [TestCase("😂", ExpectedResult = false)]       //from marketing test case
    public bool VaildAccountNameForSetProfile(string accountName) 
        => _stringVaildHelper.VaildAccountNameForSetProfile(accountName);

}
