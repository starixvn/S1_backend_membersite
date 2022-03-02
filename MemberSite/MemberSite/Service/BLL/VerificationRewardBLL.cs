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
        private readonly SelLangBLL _selLangBll;
        
        public VerificationRewardBLL(IServiceProvider provider)
        {
            _verificationRewardService = provider.GetRequiredService<IVerificationRewardService>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
        }
        public async Task<ApiResult> PhoneReward(int custId)
        {
            return await _verificationRewardService.SetVerificationReward(custId, (int) OTPType.SMS);
        }

        public async Task<ApiResult> EmailReward(int custId)
        {
            return await _verificationRewardService.SetVerificationReward(custId, (int) OTPType.Email);
        }
        
        public async Task<ApiResult> CheckRewardCondition(Customer custInfo,OTPType inputType)
        {
            var result = new ApiResult();
            
            var custRewardInfo = _verificationRewardService.GetCustVerificationReward(custInfo.CustId);
            var rewardLimit = _verificationRewardService.GetVerificationRewardLimit(custInfo.SiteId, custInfo.CurrencyId, inputType);
            if (rewardLimit != null && custRewardInfo.MaxDepositAmount < rewardLimit.MinDeposit)
            {
                var currencyName = _selLangBll.GetResourceValue($"Currency_{custInfo.CurrencyId}");
                result.ErrorCode = 104;
                result.Message = string.Format(_selLangBll.GetResourceValue("lbl_DepositLimit"),
                    currencyName,rewardLimit.MinDeposit.ToString("#,##0.##"));
                
                return result;
            }
            
            result.ErrorCode = 0;
            return result;
        }
    }
}