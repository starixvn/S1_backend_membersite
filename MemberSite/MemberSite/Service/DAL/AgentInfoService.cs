using MemberSite.Service.Model;
using Dapper;
using System.Data;
using MemberSite.Service.Library;
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
            return _reportDb.Query<ReferralSetting>("Member_ReferralSetting_Get_V3", new { _SiteId = siteId, _CurrencyId = currencyId });
        }
        public async Task<ReferralSettingDetailData> GetReferralSettingDetailAsync(int siteId, int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            return await _reportDb.QueryFirstAsync<ReferralSettingDetailData>("Member_ReferralSetting_Get_V3", p);
        }
        public void GetReferralSummary(int custId, ReferralTypeEnum type, out int referralCount, out int conditionCount, out int amountCount, out int amountSum,out int hasReferral)
        {
            var p = new DynamicParameters(new { CustId = custId });
            p.Add("ReferralCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("ConditionCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AmountCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AmountSum", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("HasReferral", dbType: DbType.Int32, direction: ParameterDirection.Output);
            if (type == ReferralTypeEnum.Referral)
                _reportDb.Execute("Member_ReferralSummary_Get", p);
            else
                _reportDb.Execute("Member_ReferralSummary_Get_V2", p);
            referralCount = p.Get<int?>("ReferralCount") ?? 0;
            conditionCount = p.Get<int?>("ConditionCount") ?? 0;
            amountCount = p.Get<int?>("AmountCount") ?? 0;
            amountSum = p.Get<int?>("AmountSum") ?? 0;
            hasReferral = p.Get<int?>("HasReferral") ?? 0;
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

        public IEnumerable<DepositReferralDao> GetDepositReferralList(int custId, long? startDate, long? endDate, int pageSize, int startIndex, ref int totalPages, ref int rowCount)
        {
            var SpName = "Member_DepositReferralList_Get";
            var props = new DynamicParameters(new { _CustId = custId });
            props.Add("_TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("_RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("_StartIndex", startIndex);
            props.Add("_PageSize", pageSize);
            props.Add("_StartDate", startDate);
            props.Add("_EndDate", endDate);

            var result = _reportDb.Query<DepositReferralDao>(SpName, props, commandType: CommandType.StoredProcedure);
            totalPages = props.Get<int?>("_TotalPages") ?? 0;
            rowCount = props.Get<int?>("_RowCount") ?? 0;

            return result;
        }

        public IEnumerable<DepositReferralDao> GetDepositReferralDetail(int custId, long? startDate, long? endDate, int pageSize, int startIndex, ref int totalPages, ref int rowCount)
        {
            var SpName = "Member_DepositReferralDetail_Get";
            var props = new DynamicParameters(new { _CustId = custId });
            props.Add("_TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("_RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("_StartIndex", startIndex);
            props.Add("_PageSize", pageSize);
            props.Add("_StartDate", startDate);
            props.Add("_EndDate", endDate);

            var result = _reportDb.Query<DepositReferralDao>(SpName, props, commandType: CommandType.StoredProcedure);
            totalPages = props.Get<int?>("_TotalPages") ?? 0;
            rowCount = props.Get<int?>("_RowCount") ?? 0;

            return result;
        }

        public bool ExistsReferralCode(int siteId, string refCode) =>
            _reportDb.ExecuteScalar<int>("Member_ExistsReferralCode_Get",
                new { _SiteId = siteId, _ReferralCode = refCode }) == 1;
    }
}
