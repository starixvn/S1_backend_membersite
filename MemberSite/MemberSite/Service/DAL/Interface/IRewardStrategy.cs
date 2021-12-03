using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL.Interface
{
    public interface IRewardStrategy
    {
        VerificationItem VerificationItem { get; }
        int PayoutVerificationReward(int custId, int siteId, int currencyId, string userName, decimal reward);
    }
}