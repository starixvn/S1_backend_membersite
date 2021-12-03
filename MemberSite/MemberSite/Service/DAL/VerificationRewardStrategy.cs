using System;
using System.Linq;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class PhoneVerificationRewardStrategy : IRewardStrategy
    {
        public VerificationItem VerificationItem => VerificationItem.SMS;
        private readonly string _connectionString;
        private readonly CustInfoService _custInfoService;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly IVerificationRewardService _verificationRewardService;

        public PhoneVerificationRewardStrategy(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
            _verificationRewardService = provider.GetRequiredService<IVerificationRewardService>();
        }
        public int PayoutVerificationReward(int custId, int siteId, int currencyId, string userName, decimal reward)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var transId = "";
            var type = 20;
            var result = 0;
            result = _custInfoService.SetPhoneVerified(custId);
            if (result <= 0) return result;
            
            result = _walletService.SetBalance(custId, siteId, userName, reward, type,
                $"phone verification reward: {reward:#,##0.##}", transId, out _, out _, conn, trans);
            if (result <= 0) return result;
            
            result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, reward, null, transId, out _, conn, trans);
            if (result <= 0) return result;

            result = _verificationRewardService.SetCustVerifiedReward(custId, siteId, currencyId, (int) VerificationItem.SMS, conn, trans);
            if (result <= 0) return result;
            
            trans.Commit();
            return result;
        }
    }

    public class EmailVerificationRewardStrategy : IRewardStrategy
    {
        public VerificationItem VerificationItem => VerificationItem.Email;
        private readonly string _connectionString;
        private readonly CustInfoService _custInfoService;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly IVerificationRewardService _verificationRewardService;

        public EmailVerificationRewardStrategy(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
            _verificationRewardService = provider.GetRequiredService<IVerificationRewardService>();
        }

        public int PayoutVerificationReward(int custId, int siteId, int currencyId, string userName, decimal reward)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var transId = "";
            var type = 20;
            var result = 0;
            result = _custInfoService.SetEmailVerified(custId);
            if (result <= 0) return result;

            result = _walletService.SetBalance(custId, siteId, userName, reward, type,
                $"email verification reward: {reward:#,##0.##}", transId, out _, out _, conn, trans);
            if (result <= 0) return result;

            result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, reward, null, transId, out _,
                conn, trans);
            if (result <= 0) return result;

            result = _verificationRewardService.SetCustVerifiedReward(custId, siteId, currencyId,
                (int) VerificationItem.Email, conn, trans);
            if (result <= 0) return result;

            trans.Commit();
            return result;
        }
    }
}