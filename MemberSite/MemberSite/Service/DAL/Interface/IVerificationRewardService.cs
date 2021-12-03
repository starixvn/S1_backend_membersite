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
        int PayoutVerificationReward(int custId, int siteId, string userName, decimal reward, VerificationType verificationType);
        int SetCustPhoneVerifiedReward(int custId, int siteId, decimal reward, MySqlConnection conn, MySqlTransaction trans);
        int SetCustEmailVerifiedReward(int custId, int siteId, decimal reward, MySqlConnection conn, MySqlTransaction trans);
    }
}