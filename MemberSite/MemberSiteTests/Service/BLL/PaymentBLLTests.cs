using MemberSite.Service.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using MemberSite.Extensions;
using Microsoft.Extensions.Configuration;
using MemberSite.Service.BLL;
using MemberSite.Service.Model.enums;
using MemberSiteTests.Stubs;

namespace MemberSiteTests.Service.BLL
{
    [TestFixture]
    public class PaymentBLLTests
    {
        private PaymentBLL _paymentBll;

        public PaymentBLLTests()
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

        [TestCase(1, 644993, 2, "[1,2]", 2, "123456", 10000, WithdrawalAccountTypeEnum.BankCard, WithdrawalInfoProcessWayEnum.Manual)] //找不到設定
        [TestCase(1, 644993, 3, "[]", 2, "222222", 10000, WithdrawalAccountTypeEnum.Crypto, WithdrawalInfoProcessWayEnum.Auto)]        //符合條件
        [TestCase(1, 644993, 3, "[]", 2, "222222", 11000, WithdrawalAccountTypeEnum.Crypto, WithdrawalInfoProcessWayEnum.Manual)]      //金額超過
        [TestCase(1, 644993, 3, "[333]", 2, "222222", 10000, WithdrawalAccountTypeEnum.BankCard, WithdrawalInfoProcessWayEnum.Manual)] //AccountType不支援
        [TestCase(1, 644993, 3, "[1]", 2, "222222", 10000, WithdrawalAccountTypeEnum.Crypto, WithdrawalInfoProcessWayEnum.Manual)]   //Tag不符合
        [TestCase(1, 250994, 3, "[]", 2, "0x23E717D9401C96cD987fCc746Ed998de83f15Ccf", 10000, WithdrawalAccountTypeEnum.Crypto, WithdrawalInfoProcessWayEnum.Manual)]   //SuccessCount不夠

        public void Test_GetWithdrawalProcessWay(int siteId, int custId, int currencyId, string tagList, int vipLevel, string accountNo, decimal withdrawalAmount,
            WithdrawalAccountTypeEnum accountType, WithdrawalInfoProcessWayEnum expected)
        {
            var custInfo = new Customer() { SiteId = siteId, CustId = custId, CurrencyId = currencyId, TagList = tagList, UserLevel = vipLevel };
            var actual = _paymentBll.GetWithdrawalProcessWay(custInfo, accountNo, withdrawalAmount, accountType);
            Assert.AreEqual(actual, expected);
        }
    }
}
