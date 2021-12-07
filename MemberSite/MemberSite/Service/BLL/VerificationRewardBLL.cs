using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.BLL
{
    public class VerificationRewardBLL
    {
        private readonly IVerificationRewardService _verificationRewardService;
        private readonly IEnumerable<IRewardStrategy> _strategies;
        public VerificationRewardBLL(IServiceProvider provider)
        {
            _strategies = provider.GetServices<IRewardStrategy>();
            _verificationRewardService = provider.GetRequiredService<IVerificationRewardService>();
        }
        public async Task PhoneReward(int custId)
        {
            var custReward = await _verificationRewardService.GetCustVerificationReward(custId);
            if(custReward.PhoneRewardTime.HasValue)
                return;
            var verificationReward = await _verificationRewardService.GetVerificationReward(custReward.SiteId, custReward.CurrencyId);
            
            var rewardStrategy = CreateStrategy(VerificationItem.SMS);
            if(verificationReward.VerificationType == 0)
            {
                rewardStrategy.PayoutVerificationReward(custReward.CustId, custReward.SiteId, custReward.CurrencyId, 
                    custReward.UserName, verificationReward.PhoneReward, $"phone verification reward: {verificationReward.PhoneReward:#,##0.##}");
            }
            else if(verificationReward.VerificationType == 1 && custReward.EmailVerified)
            {
                rewardStrategy.PayoutVerificationReward(custReward.CustId, custReward.SiteId, custReward.CurrencyId, 
                    custReward.UserName, verificationReward.PhoneReward, $"phone & email verification reward: {verificationReward.PhoneReward:#,##0.##}");
            }
        }

        public async Task EmailReward(int custId)
        {
            var custReward = await _verificationRewardService.GetCustVerificationReward(custId);
            if(custReward.EmailRewardTime.HasValue)
                return;
            var verificationReward = await _verificationRewardService.GetVerificationReward(custReward.SiteId, custReward.CurrencyId);
            var rewardStrategy = CreateStrategy(VerificationItem.Email);
            if(verificationReward.VerificationType == 0)
            {
                rewardStrategy.PayoutVerificationReward(custReward.CustId, custReward.SiteId, custReward.CurrencyId,
                    custReward.UserName, verificationReward.EmailReward, $"email verification reward: {verificationReward.EmailReward:#,##0.##}");
            }
            else if (verificationReward.VerificationType == 1 && custReward.PhoneVerified)
            {
                rewardStrategy.PayoutVerificationReward(custReward.CustId, custReward.SiteId, custReward.CurrencyId, 
                    custReward.UserName, verificationReward.PhoneReward, $"phone & email verification reward: {verificationReward.PhoneReward:#,##0.##}");
            }
        }

        private IRewardStrategy CreateStrategy(VerificationItem item)
        {
            return _strategies.First(a => a.VerificationItem == item);
        }
    }
}