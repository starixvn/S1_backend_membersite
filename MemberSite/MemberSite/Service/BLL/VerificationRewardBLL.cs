using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace MemberSite.Service.BLL
{
    public class VerificationRewardBLL
    {
        private readonly IVerificationRewardService _verificationRewardService;
        public VerificationRewardBLL(IServiceProvider provider)
        {
            _verificationRewardService = provider.GetRequiredService<IVerificationRewardService>();
        }
        public async Task PhoneReward(int custId)
        {
            await _verificationRewardService.SetVerificationReward(custId, (int)OTPType.SMS);
        }

        public async Task EmailReward(int custId)
        {
            await _verificationRewardService.SetVerificationReward(custId, (int)OTPType.Email);
        }
    }
}