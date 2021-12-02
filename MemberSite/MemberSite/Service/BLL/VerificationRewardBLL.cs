using System;
using System.Threading.Tasks;
using MemberSite.Service.DAL.Interface;
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
            // 取得用戶驗證狀況
            var custReward = await _verificationRewardService.GetCustVerificationReward(custId);
            // 檢查是否領過
            if(custReward.MailRewardTime.HasValue)
                return;
            // 取得設定
            var verificationReward = await _verificationRewardService.GetVerificationReward(custReward.SiteId, custReward.CurrencyId);
            if(verificationReward.VerificationType == 0 ||
               (verificationReward.VerificationType == 1 && custReward.EmailVerified))
            {
                _verificationRewardService.GiveVerificationReward(custReward.CustId, custReward.SiteId, custReward.UserName, verificationReward.PhoneReward, 0);
            }
        }

        public async Task EmailReward(int custId)
        {
            // 取得用戶驗證狀況
            // 檢查是否領過
            // 取得設定
            // case 兩者需都驗證 且 Phone已驗證
            // case 只須通過一驗證
                // 設定已驗證
                // 更新錢包
                // 設定提領限額
                // 寫入領取紀錄紀錄
        }
    }
}