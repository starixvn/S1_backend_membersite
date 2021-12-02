using System.Threading.Tasks;
using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IVerificationRewardService
    {
        Task<CustVerificationRewardData> GetCustVerificationReward(int custId);
        Task<VerificationRewardData> GetVerificationReward(int siteId, int currencyId);
        void GiveVerificationReward(int custId, int siteId, string userName, decimal reward, int verificationType);
    }
}