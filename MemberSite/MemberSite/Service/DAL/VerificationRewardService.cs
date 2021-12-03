using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class VerificationRewardService : IVerificationRewardService
    {
        private readonly string _connectionString;
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;
        private readonly CustInfoService _custInfoService;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        public VerificationRewardService(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            _mainDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
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

        public int SetCustPhoneVerifiedReward(int custId, int siteId, decimal reward, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new {_CustId = custId, _SiteId = siteId, _PhoneReward = reward});
            return _mainDb.Execute("Member_PhoneReward_Set", p, conn, trans);
        }
        
        public int SetCustEmailVerifiedReward(int custId, int siteId, decimal reward, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new {_CustId = custId, _SiteId = siteId, _EmailReward = reward});
            return _mainDb.Execute("Member_EmailReward_Set", p, conn, trans);
        }

        public int PayoutVerificationReward(int custId, int siteId, string userName, decimal reward, VerificationType verificationType)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var transId = "";
            var type = 20;
            var result = 0;
            if(verificationType == VerificationType.SMS) result = _custInfoService.SetPhoneVerified(custId);
            else if(verificationType == VerificationType.Email) result = _custInfoService.SetEmailVerified(custId);
            if (result <= 0) return result;
            
            result = _walletService.SetBalance(custId, siteId, userName, reward, type,$"phone verification reward: {reward:#,##0.##}", transId, out _, out _, conn, trans);
            if (result <= 0) return result;
            result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, reward, null, transId, out _, conn, trans);
            if (result <= 0) return result;

            if(verificationType == VerificationType.SMS) result = SetCustPhoneVerifiedReward(custId, siteId, reward, conn, trans);
            else if (verificationType == VerificationType.Email) result = SetCustEmailVerifiedReward(custId, siteId, reward, conn, trans);
            if (result <= 0) return result;
            
            trans.Commit();
            return result;
        }
    }
}