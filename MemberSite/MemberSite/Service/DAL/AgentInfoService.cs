using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using Dapper;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class AgentInfoService
    {
        private readonly IConfiguration configuration;

        public AgentInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public Customer GetSuperAgent(int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirst<Customer>("SuperAgent_Get", new { CurrencyId = currencyId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public Customer GetAffiliateAgent(string affiliateId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<Customer>("AffiliateAgent_Get", new { AffiliateId = affiliateId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public int AddAffiliateId(string affiliateId, int custId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_Affiliate_Ins", new { AffiliateId = affiliateId, CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public string GetAffiliateId(int custId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<string>("Member_AffiliateId_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<ReferralSetting> GetReferralSetting(int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<ReferralSetting>("Member_ReferralSetting_Get", new { SiteId = siteId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
           
        }
        public void GetReferralSummary(int custId, out int referralCount, out int conditionCount, out int amountCount, out int amountSum)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { CustId = custId});
            p.Add("ReferralCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("ConditionCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AmountCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("AmountSum", dbType: DbType.Int32, direction: ParameterDirection.Output);
            conn.Execute("Member_ReferralSummary_Get", p, commandType: CommandType.StoredProcedure);

            referralCount = p.Get<int?>("ReferralCount") ?? 0;
            conditionCount = p.Get<int?>("ConditionCount") ?? 0;
            amountCount = p.Get<int?>("AmountCount") ?? 0;
            amountSum = p.Get<int?>("AmountSum") ?? 0;
        }
        public IEnumerable<ReferralFriend> GetRefFriendList(int custId, long? StartDate, long? EndDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var SpName = "Member_ReferralList_Get_V2";
            var props = new DynamicParameters(new { CustId = custId });
            props.Add("PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            props.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("PageSize", PageSize);
            props.Add("StartDate", StartDate);
            props.Add("EndDate", EndDate);

            var result = conn.Query<ReferralFriend>(SpName, props, commandType: CommandType.StoredProcedure);
            PageNumber = props.Get<int?>("PageNumber") ?? 0;
            TotalPages = props.Get<int?>("TotalPages") ?? 0;
            RowCount = props.Get<int?>("RowCount") ?? 0;
            
            return result;
        }
    }
}
