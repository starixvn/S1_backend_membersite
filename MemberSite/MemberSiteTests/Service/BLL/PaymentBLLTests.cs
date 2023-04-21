using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemberSite.Extensions;
using Microsoft.Extensions.Configuration;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MemberSite;
using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using MemberSite.Service.DAL;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Options;
using MemberSiteTests.Stubs;

namespace MemberSiteTests.Service.BLL
{
    [TestFixture]
    public class PaymentBLLTests
    {
        private PaymentBLL _paymentBll;

        [SetUp]
        public void SetUp()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });
            builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());
            var startup = new StubStartUp(builder.Configuration);
            startup.ConfigureServices(builder.Services);
            var app = builder.Build();
            _paymentBll = app.Services.GetRequiredService<PaymentBLL>();
        }

        [TestCase(1, 644993, 2, "[1,2]", 1, "123456", 10000, WithdrawalAccountTypeEnum.BankCard, WithdrawalInfoProcessWayEnum.Manual)]
        [TestCase(1, 644993, 3, "[]", 1, "222222", 10000, WithdrawalAccountTypeEnum.Crypto, WithdrawalInfoProcessWayEnum.Auto)]


        public void Test_GetWithdrawalProcessWay(int siteId, int custId, int currencyId, string tagList, int vipLevel, string accountNo, decimal withdrawalAmount,
            WithdrawalAccountTypeEnum accountType, WithdrawalInfoProcessWayEnum expected)
        {
            var custInfo = new Customer() { SiteId = siteId, CustId = custId, CurrencyId = currencyId, TagList = tagList, UserLevel = vipLevel };
            var actual = _paymentBll.GetWithdrawalProcessWay(custInfo, accountNo, withdrawalAmount, accountType);
            Assert.AreEqual(actual, expected);
        }
    }
}
