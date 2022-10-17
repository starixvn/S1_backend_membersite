using FluentAssertions;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture]
    public class WithdrawLimitTests
    {
        [Test]
        public void EWallet_In_Maintenance()
        {
            var ewalletInfo = new EWalletWithdrawalLimitInfoOutput()
            {
                Status = EWalletStatus.Disable,
                WithdrawalMin = 10,
                WithdrawalMax = 300
            };
            var withdrawalLimitInfo = new WithdrawalLimitInfoOutput() { };
            var dailyCustomerWithdrawalInfo = new DailyCustomerWithdrawalInfoOutput() { };
            var amount = 150m;
            
            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(ewalletInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, amount);

            errorCode.Should().Be(-8);
        }
        
        [Test]
        public void EWallet_withdraw_amount_less_than_withdraw_limit_min()
        {
            var ewalletInfo = new EWalletWithdrawalLimitInfoOutput()
            {
                Status = EWalletStatus.Able,
                WithdrawalMin = 10,
                WithdrawalMax = 300
            };
            var withdrawalLimitInfo = new WithdrawalLimitInfoOutput()
            {
                WithdrawalMin = 20,
                TotalWithdrawalMax = 700,
                Times = 5
            };
            var dailyCustomerWithdrawalInfo = new DailyCustomerWithdrawalInfoOutput() { };
            var amount = 15m;
            
            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(ewalletInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, amount);

            errorCode.Should().Be(-5);
        }
        
        [Test]
        public void EWallet_withdraw_less_than_ewallet_setting_min()
        {
            var ewalletInfo = new EWalletWithdrawalLimitInfoOutput()
            {
                Status = EWalletStatus.Able,
                WithdrawalMin = 30,
                WithdrawalMax = 300
            };
            var withdrawalLimitInfo = new WithdrawalLimitInfoOutput()
            {
                WithdrawalMin = 20,
                TotalWithdrawalMax = 700,
                Times = 5
            };
            var dailyCustomerWithdrawalInfo = new DailyCustomerWithdrawalInfoOutput() { };
            var amount = 25m;
            
            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(ewalletInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, amount);

            errorCode.Should().Be(-5);
        }
        
        [Test]
        public void EWallet_withdraw_greater_than_ewallet_setting_max()
        {
            var ewalletInfo = new EWalletWithdrawalLimitInfoOutput()
            {
                Status = EWalletStatus.Able,
                WithdrawalMin = 30,
                WithdrawalMax = 300
            };
            var withdrawalLimitInfo = new WithdrawalLimitInfoOutput()
            {
                WithdrawalMin = 20,
                TotalWithdrawalMax = 700,
                Times = 5
            };
            var dailyCustomerWithdrawalInfo = new DailyCustomerWithdrawalInfoOutput() { };
            var amount = 301m;
            
            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(ewalletInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, amount);

            errorCode.Should().Be(-9);
        }
        
        [Test]
        public void EWallet_withdraw_greater_than_withdraw_limit_daily_total_max()
        {
            var ewalletInfo = new EWalletWithdrawalLimitInfoOutput()
            {
                Status = EWalletStatus.Able,
                WithdrawalMin = 30,
                WithdrawalMax = 300
            };
            var withdrawalLimitInfo = new WithdrawalLimitInfoOutput()
            {
                WithdrawalMin = 20,
                TotalWithdrawalMax = 700,
                Times = 5
            };
            var dailyCustomerWithdrawalInfo = new DailyCustomerWithdrawalInfoOutput()
            {
                Amount = 650,
                Count = 3
            };
            var amount = 100m;
            
            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(ewalletInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, amount);

            errorCode.Should().Be(-6);
        }
        
        [Test]
        public void EWallet_withdraw_greater_than_withdraw_limit_daily_total_count()
        {
            var ewalletInfo = new EWalletWithdrawalLimitInfoOutput()
            {
                Status = EWalletStatus.Able,
                WithdrawalMin = 30,
                WithdrawalMax = 300
            };
            var withdrawalLimitInfo = new WithdrawalLimitInfoOutput()
            {
                WithdrawalMin = 20,
                TotalWithdrawalMax = 700,
                Times = 5
            };
            var dailyCustomerWithdrawalInfo = new DailyCustomerWithdrawalInfoOutput()
            {
                Amount = 150,
                Count = 5
            };
            var amount = 100m;
            
            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(ewalletInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, amount);

            errorCode.Should().Be(-7);
        }
        
        [Test]
        public void EWallet_withdraw_and_in_limit()
        {
            var ewalletInfo = new EWalletWithdrawalLimitInfoOutput()
            {
                Status = EWalletStatus.Able,
                WithdrawalMin = 30,
                WithdrawalMax = 300
            };
            var withdrawalLimitInfo = new WithdrawalLimitInfoOutput()
            {
                WithdrawalMin = 20,
                TotalWithdrawalMax = 700,
                Times = 5
            };
            var dailyCustomerWithdrawalInfo = new DailyCustomerWithdrawalInfoOutput()
            {
                Amount = 300,
                Count = 1
            };
            var amount = 100m;
            
            var withdrawLimit = WithdrawLimitFactory.Create(WithdrawalTypeStatus.EWallet);
            var errorCode = withdrawLimit.CheckWithdrawLimit(ewalletInfo, withdrawalLimitInfo, dailyCustomerWithdrawalInfo, amount);

            errorCode.Should().Be(0);
        }
    }
}