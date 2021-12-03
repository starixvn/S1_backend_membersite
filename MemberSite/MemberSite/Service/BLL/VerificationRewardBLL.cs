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
            
            if(verificationReward.VerificationType == 0 ||
               (verificationReward.VerificationType == 1 && custReward.EmailVerified))
            {
                var rewardStrategy = CreateStrategy(VerificationItem.SMS);
                rewardStrategy.PayoutVerificationReward(custReward.CustId, custReward.SiteId, custReward.CurrencyId, custReward.UserName, verificationReward.PhoneReward);
            }
        }

        public async Task EmailReward(int custId)
        {
            var custReward = await _verificationRewardService.GetCustVerificationReward(custId);
            if(custReward.EmailRewardTime.HasValue)
                return;
            var verificationReward = await _verificationRewardService.GetVerificationReward(custReward.SiteId, custReward.CurrencyId);
            
            if(verificationReward.VerificationType == 0 ||
               (verificationReward.VerificationType == 1 && custReward.PhoneVerified))
            {
                var rewardStrategy = CreateStrategy(VerificationItem.Email);
                rewardStrategy.PayoutVerificationReward(custReward.CustId, custReward.SiteId, custReward.CurrencyId, custReward.UserName, verificationReward.EmailReward);
            }
        }

        private IRewardStrategy CreateStrategy(VerificationItem item)
        {
            return _strategies.First(a => a.VerificationItem == item);
        }
    }
}