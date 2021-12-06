using System.Threading.Tasks;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL.Interface
{
    public interface IVerificationRewardService
    {
        Task<CustVerificationRewardData> GetCustVerificationReward(int custId);
        Task<VerificationRewardData> GetVerificationReward(int siteId, int currencyId);
        int SetCustVerifiedReward(int custId, int siteId, int currencyId, int verifyItem, MySqlConnection conn, MySqlTransaction trans);
    }
}