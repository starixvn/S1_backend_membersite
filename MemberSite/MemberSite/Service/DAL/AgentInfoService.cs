using System.Data;

using Dapper;

using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class AgentInfoService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDb;

        public AgentInfoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public Customer GetSuperAgent(int siteId, int currencyId)
        {
            return _reportDb.QueryFirst<Customer>("SuperAgent_Get", new { CurrencyId = currencyId, SiteId = siteId });
        }
        public Customer GetAffiliateAgent(string affiliateId, int siteId)
        {
            return _reportDb.QueryFirst<Customer>("AffiliateAgent_Get", new { AffiliateId = affiliateId, SiteId = siteId });
        }
        public int AddAffiliateId(string affiliateId, int custId, int siteId)
        {
            return _mainDbClient.Execute("Member_Affiliate_Ins", new { AffiliateId = affiliateId, CustId = custId, SiteId = siteId });
        }
        public string GetAffiliateId(int custId, int siteId, DataBase dataBase = DataBase.Report)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.QueryFirst<string>("Member_AffiliateId_Get", new { CustId = custId, SiteId = siteId }),
                _ => _reportDb.QueryFirst<string>("Member_AffiliateId_Get", new { CustId = custId, SiteId = siteId }),
            };
        }
        public IEnumerable<ReferralSetting> GetReferralSetting(int siteId, int currencyId)
        {
            return _reportDb.Query<ReferralSetting>("Member_ReferralSetting_Get", new { SiteId = siteId, CurrencyId = currencyId });
        }
        public async Task<ReferralSettingDetailData> GetReferralSettingDetailAsync(int siteId, int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            return await _reportDb.QueryFirstAsync<ReferralSettingDetailData>("Member_ReferralSetting_Get_V2", p);
        }
        public void GetReferralSummary(int custId, out int referralCount, out int conditionCount, out int amountCount, out int amountSum)
        {
            var p = new DynamicParameters(new { CustId = custId });
            p.Add("ReferralCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("ConditionCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AmountCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AmountSum", dbType: DbType.Int32, direction: ParameterDirection.Output);
            _reportDb.Execute("Member_ReferralSummary_Get", p);

            referralCount = p.Get<int?>("ReferralCount") ?? 0;
            conditionCount = p.Get<int?>("ConditionCount") ?? 0;
            amountCount = p.Get<int?>("AmountCount") ?? 0;
            amountSum = p.Get<int?>("AmountSum") ?? 0;
        }
        public IEnumerable<ReferralFriend> GetRefFriendList(int custId, long? StartDate, long? EndDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            var SpName = "Member_ReferralList_Get_V2";
            var props = new DynamicParameters(new { CustId = custId });
            props.Add("PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            props.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("PageSize", PageSize);
            props.Add("StartDate", StartDate);
            props.Add("EndDate", EndDate);

            var result = _reportDb.Query<ReferralFriend>(SpName, props, commandType: CommandType.StoredProcedure);
            PageNumber = props.Get<int?>("PageNumber") ?? 0;
            TotalPages = props.Get<int?>("TotalPages") ?? 0;
            RowCount = props.Get<int?>("RowCount") ?? 0;

            return result;
        }
    }
}
