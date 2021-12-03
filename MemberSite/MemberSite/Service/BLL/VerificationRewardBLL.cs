using System;
using System.Threading.Tasks;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.BLL
{
    public class VerificationRewardBLL
    {
        private IVerificationRewardService _verificationRewardService;
        public VerificationRewardBLL(IServiceProvider provider)
        {
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
                _verificationRewardService.PayoutVerificationReward(custReward.CustId, custReward.SiteId, custReward.UserName, verificationReward.PhoneReward, VerificationType.SMS);
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
                _verificationRewardService.PayoutVerificationReward(custReward.CustId, custReward.SiteId, custReward.UserName, verificationReward.EmailReward, VerificationType.Email);
            }
        }
    }
}