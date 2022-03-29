using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IVerificationRewardService
    {
        Task<ApiResult> SetVerificationReward(int custId, int verificationType);
    }
}