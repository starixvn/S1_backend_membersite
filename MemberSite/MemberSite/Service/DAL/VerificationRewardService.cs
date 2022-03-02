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

namespace MemberSite.Service.DAL
{
    public class VerificationRewardService : IVerificationRewardService
    {
        private readonly CourierAdapter _courierAdapter;
        private readonly string _bonusServiceUrl;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        public VerificationRewardService(IServiceProvider provider)
        {
            _courierAdapter = provider.GetService<CourierAdapter>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            _bonusServiceUrl = configuration.GetValue<string>("BonusService");
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public async Task<ApiResult> SetVerificationReward(int custId, int verificationType)
        {
            var apiUrl = $"{_bonusServiceUrl}SetVerificationReward";

            var req = new VerificationRewardObj
            {
                CustId = custId,
                OTPType = verificationType
            };

            var result = await _courierAdapter.MyPostAsync<ApiResult, VerificationRewardObj>(apiUrl, req);

            return result;
        }
        
        public CustVerificationRewardData GetCustVerificationReward(int custId)
        {
            var p = new DynamicParameters(new { _CustId = custId });
            return _mainDbClient.QueryFirst<CustVerificationRewardData>("Service_CustVerificationReward_Get_V2", p);

        }
        public VerificationRewardLimit GetVerificationRewardLimit(int siteId ,int currencyId ,OTPType inputType)
        {
            var p = new DynamicParameters(new { _SiteId = siteId, _CurrencyId = currencyId , _Type = (int)inputType});
            return _reportDbClient.QueryFirst<VerificationRewardLimit>("Service_VerificationRewardLimit_Get", p);
        }
        
        private class VerificationRewardObj
        {
            public int CustId { get; set; }
            public int OTPType { get; set; }
        }
    }
}