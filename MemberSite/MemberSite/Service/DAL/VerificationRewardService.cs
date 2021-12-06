using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class VerificationRewardService : IVerificationRewardService
    {
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;
        public VerificationRewardService(IServiceProvider provider)
        {
            _mainDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public async Task<CustVerificationRewardData> GetCustVerificationReward(int custId)
        {
            var p = new DynamicParameters(new {_CustId = custId});
            return await _reportDb.QueryFirstAsync<CustVerificationRewardData>("Member_CustVerificationReward_Get", p);
        }

        public async Task<VerificationRewardData> GetVerificationReward(int siteId, int currencyId)
        {
            var p = new DynamicParameters(new {_SiteId = siteId, _CurrencyId = currencyId});
            return await _reportDb.QueryFirstAsync<VerificationRewardData>("Member_VerificationReward_Get", p);
        }
        
        public int SetCustVerifiedReward(int custId, int siteId, int currencyId,int verifyItem, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new {_CustId = custId, _SiteId = siteId, _CurrencyId = currencyId, _VerificationItem = verifyItem});
            return _mainDb.Execute("Member_CustVerificationReward_Set", p, conn, trans);
        }
    }
}