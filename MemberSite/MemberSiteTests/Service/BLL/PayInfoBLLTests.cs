using System.Linq;

using MemberSite.Extensions;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture()]
    public class PayInfoBLLTests
    {
        private readonly IConfiguration _configuration;
        private readonly PayInfoBLL _payInfoBLL;
        public PayInfoBLLTests()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "LOCAL",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            builder.Services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            builder.Services.AddSingleton(p => Substitute.For<IWebHostEnvironment>());
            builder.Services.AddSingleton(p => Substitute.For<ILogger<CourierAdapter>>());

            var startup = new Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);
            var app = builder.Build();

            _configuration = app.Configuration;
            _payInfoBLL = app.Services.GetRequiredService<PayInfoBLL>();
        }


        [TestCase(251048, DepositType.MobilePayment, ExpectedResult = true)]
        [TestCase(251048, DepositType.QrPayPayment, ExpectedResult = true)]
        public bool GetPayGroupListTest(int custId, DepositType depositType) => _payInfoBLL.GetPayGroupList(custId, depositType).Any();

        [TestCase(251048, 2, DepositType.MobilePayment, ExpectedResult = true)]
        [TestCase(251048, 29, DepositType.QrPayPayment, ExpectedResult = true)]
        public bool GetPayInfoListTest(int custId, int groupId, DepositType depositType) => _payInfoBLL.GetPayInfoList(custId, groupId, depositType).Any();
    }
}