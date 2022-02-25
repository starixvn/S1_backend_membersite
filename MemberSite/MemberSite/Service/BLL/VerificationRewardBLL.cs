using System;
using System.Threading.Tasks;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;
namespace MemberSite.Service.BLL
{
    public class VerificationRewardBLL
    {
        private readonly IVerificationRewardService _verificationRewardService;
        public VerificationRewardBLL(IServiceProvider provider)
        {
            _verificationRewardService = provider.GetRequiredService<IVerificationRewardService>();
        }
        public async Task<ApiResult> PhoneReward(int custId)
        {
            return await _verificationRewardService.SetVerificationReward(custId, (int) OTPType.SMS);
        }

        public async Task<ApiResult> EmailReward(int custId)
        {
            return await _verificationRewardService.SetVerificationReward(custId, (int) OTPType.Email);
        }
    }
}