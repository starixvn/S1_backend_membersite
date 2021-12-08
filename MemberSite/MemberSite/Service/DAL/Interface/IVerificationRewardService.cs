using System.Threading.Tasks;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL.Interface
{
    public interface IVerificationRewardService
    {
        Task<ApiResult> SetVerificationReward(int custId, int verificationType);
    }
}