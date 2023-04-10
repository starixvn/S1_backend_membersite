using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using MemberSite.Service.Library;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using MemberSite.Service.Model.enums;
using Microsoft.AspNetCore.Hosting;
using FluentAssertions;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Builder;
using MemberSite.Extensions;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture()]
    public class PayInfoBLLTests
    {
        private readonly IConfiguration _configuration;
        private readonly PayInfoBLL _payInfoBLL;
        private readonly MobilePayInfoBll _mobilePayInfoBLL;
        public PayInfoBLLTests()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });
            builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());
            builder.Services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            builder.Services.AddSingleton(p => Substitute.For<IWebHostEnvironment>());
            builder.Services.AddSingleton(p => Substitute.For<ILogger<CourierAdapter>>());

            var startup = new Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);
            var app = builder.Build();

            _configuration = app.Configuration;
            _payInfoBLL = app.Services.GetRequiredService<PayInfoBLL>();
            _mobilePayInfoBLL = app.Services.GetRequiredService<MobilePayInfoBll>();
        }


        [TestCase(251048, DepositType.MobilePayment, ExpectedResult = true)]
        [TestCase(251048, DepositType.QRPayPayment, ExpectedResult = true)]
        public bool GetPayGroupListTest(int custId, DepositType depositType) => _payInfoBLL.GetPayGroupList(custId, depositType).Any();

        [TestCase(251048, 2, DepositType.MobilePayment, ExpectedResult = true)]
        [TestCase(251048, 29, DepositType.QRPayPayment, ExpectedResult = true)]
        public bool GetPayInfoListTest(int custId, int groupId, DepositType depositType) => _payInfoBLL.GetPayInfoList(custId, groupId, depositType).Any();

        [TestCase(251048)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(35)]
        [TestCase(36)]
        [TestCase(37)]
        [TestCase(1341)]
        [TestCase(250052)]
        public void PayGroupTest(int custId)
        {
            var payGroup = _payInfoBLL.GetPayGroupList(custId, DepositType.MobilePayment);
            var mobilePayGroup = _mobilePayInfoBLL.GetMobilePayGroupList(custId);
            var payString = JsonConvert.SerializeObject(payGroup);
            var mobilePayString = JsonConvert.SerializeObject(mobilePayGroup);

            payString.Should().BeEquivalentTo(mobilePayString);
        }


        [TestCase(36, 16)]
        [TestCase(36, 17)]
        [TestCase(36, 31)]
        [TestCase(1, 5)]
        [TestCase(1, 34)]
        [TestCase(35, 6)]
        [TestCase(35, 7)]
        [TestCase(35, 8)]
        [TestCase(35, 9)]
        [TestCase(35, 10)]
        [TestCase(35, 12)]
        [TestCase(37, 21)]
        [TestCase(37, 22)]
        [TestCase(37, 23)]
        [TestCase(37, 24)]
        [TestCase(37, 25)]
        [TestCase(37, 26)]
        [TestCase(37, 27)]
        [TestCase(250052, 28)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        [TestCase(2, 4)]
        [TestCase(2, 19)]
        [TestCase(2, 38)]
        [TestCase(2, 39)]
        public void PayInfoTest(int custId, int groupId)
        {
            var payInfo = _payInfoBLL.GetPayInfoList(custId, groupId, DepositType.MobilePayment);
            var mobilePayInfo = _mobilePayInfoBLL.GetMobilePayInfoList(custId, groupId);
            var payString = JsonConvert.SerializeObject(payInfo);
            var mobilePayString = JsonConvert.SerializeObject(mobilePayInfo);

            payString.Should().BeEquivalentTo(mobilePayString);
        }

        [TestCase(36, 60)]
        [TestCase(36, 61)]
        [TestCase(36, 82)]
        [TestCase(1, 8)]
        [TestCase(1, 41)]
        [TestCase(1, 43)]
        [TestCase(1, 79)]
        [TestCase(1, 85)]
        [TestCase(1, 88)]
        [TestCase(35, 11)]
        [TestCase(35, 12)]
        [TestCase(35, 13)]
        [TestCase(35, 14)]
        [TestCase(35, 15)]
        [TestCase(35, 17)]
        [TestCase(35, 20)]
        [TestCase(35, 26)]
        [TestCase(35, 27)]
        [TestCase(35, 28)]
        [TestCase(35, 30)]
        [TestCase(35, 31)]
        [TestCase(35, 32)]
        [TestCase(35, 33)]
        [TestCase(35, 34)]
        [TestCase(35, 35)]
        [TestCase(35, 37)]
        [TestCase(35, 38)]
        [TestCase(35, 39)]
        [TestCase(35, 46)]
        [TestCase(37, 71)]
        [TestCase(37, 72)]
        [TestCase(37, 73)]
        [TestCase(37, 74)]
        [TestCase(37, 75)]
        [TestCase(37, 76)]
        [TestCase(37, 77)]
        [TestCase(37, 108)]
        [TestCase(37, 109)]
        [TestCase(37, 110)]
        [TestCase(37, 111)]
        [TestCase(37, 112)]
        [TestCase(37, 113)]
        [TestCase(37, 114)]
        [TestCase(250052, 48)]
        [TestCase(250052, 49)]
        [TestCase(250052, 50)]
        [TestCase(250052, 53)]
        [TestCase(250052, 56)]
        [TestCase(250052, 58)]
        [TestCase(250052, 62)]
        [TestCase(250052, 63)]
        [TestCase(250052, 65)]
        [TestCase(250052, 86)]
        [TestCase(250052, 92)]
        [TestCase(250052, 93)]
        [TestCase(250052, 98)]
        public void DepositTest(int custId, int channelId)
        {
            var result1 = _payInfoBLL.DepositReqPay(custId, channelId, 100, out var _, out var payInfo1, DepositType.MobilePayment);
            var result2 = _mobilePayInfoBLL.DepositReqMobilePay(custId, channelId, 100, out var _, out var payInfo2);
            var pay1 = JsonConvert.SerializeObject(payInfo1);
            var pay2 = JsonConvert.SerializeObject(payInfo2);

            result1.Should().Be(result2);
            pay1.Should().BeEquivalentTo(pay2);
        }

    }
}