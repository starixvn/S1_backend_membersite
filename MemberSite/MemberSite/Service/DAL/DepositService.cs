using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using Dapper;
using MemberSite.Service.Library;
using System.Data;
using MemberSite.Service.BLL;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class DepositService
    {
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly MySqlClient _mainDbClient;
        private readonly ShieldBLL _shieldBll;

        public DepositService(IServiceProvider provider)
        {
            this.configuration = provider.GetRequiredService<IConfiguration>();
            this.common = provider.GetService<Common>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _shieldBll = provider.GetRequiredService<ShieldBLL>();
        }
        public int DepositReq(DepositDetailInfo depositDetailInfo, out string transId, out string postScript)
        {
            transId = null;
            postScript = null;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { depositDetailInfo.DepositType, depositDetailInfo.CustId, depositDetailInfo.UserName, depositDetailInfo.CurrencyId, depositDetailInfo.SiteId, depositDetailInfo.DepositInfo, depositDetailInfo.Amount, depositDetailInfo.Memo, Prefix = $"D{common.GetEnv()}" });
            p.Add("vPostScript", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            var result = conn.Execute("DepositInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();

            if (depositDetailInfo is { SiteId: (int)SiteId.Goal123 })
            {
                _ = Task.Run(async () =>
                {
                    var checkResult = _shieldBll.CheckCustShieldRisk(
                        new Customer()
                        {
                            SiteId = depositDetailInfo.SiteId,
                            CurrencyId = depositDetailInfo.CurrencyId,
                            CustId = depositDetailInfo.CustId
                        },
                        ShieldRiskTriggeredPoint.Deposit,
                        ShieldRiskTaggedIssue.HighRisk);
                });
            }

            postScript = p.Get<string>("vPostScript");
            transId = p.Get<string>("vTransId");

            return result;
        }
        public int DepositReqwithFrom(DepositDetailInfo depositDetailInfo, string depositFrom, out string transId, out string postScript)
        {
            transId = null;
            postScript = null;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { depositDetailInfo.DepositType, depositDetailInfo.CustId, depositDetailInfo.UserName, depositDetailInfo.CurrencyId, depositDetailInfo.SiteId, depositFrom, depositDetailInfo.DepositInfo, depositDetailInfo.Amount, depositDetailInfo.Memo, Prefix = $"D{common.GetEnv()}" });
            p.Add("vPostScript", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            var result = conn.Execute("DepositInfoWithFrom_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();

            if (depositDetailInfo is { SiteId: (int)SiteId.Goal123 })
            {
                _ = Task.Run(async () =>
                {
                    var checkResult = _shieldBll.CheckCustShieldRisk(
                        new Customer()
                        {
                            SiteId = depositDetailInfo.SiteId,
                            CurrencyId = depositDetailInfo.CurrencyId,
                            CustId = depositDetailInfo.CustId
                        },
                        ShieldRiskTriggeredPoint.Deposit,
                        ShieldRiskTaggedIssue.HighRisk);
                });
            }

            postScript = p.Get<string>("vPostScript");
            transId = p.Get<string>("vTransId");

            return result;
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoRecent(int custId, int siteId, int topNum)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositInfoHistory>("DepositInfoRecent_Get_V2", new { CustId = custId, SiteId = siteId, TopNum = topNum }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate, SiteId = siteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var depositInfoHistoryResult = conn.Query<DepositInfoHistory>("DepositInfoSearch_Get_V2", p, commandType: CommandType.StoredProcedure);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return depositInfoHistoryResult;
        }
        public int SetDepositReceipt(int custId, int depositId, string refNo, long depositTime)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("DepositReceipt_Set", new { CustId = custId, DepositId = depositId, RefNo = refNo, DepositTime = depositTime }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> SetDepositReceiptPic(int custId, int depositId, string picFileName, byte[] depositPic)
        {
            var p = new DynamicParameters(new
            { CustId = custId, DepositId = depositId, PicFileName = picFileName, DepositPic = depositPic });
            return await _mainDbClient.ExecuteAsync("DepositReceiptPic_Set", p);
        }
        public int SetDepositCancel(int custId, int depositId, int siteid)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = conn.Execute("DepositInfoCancel_Set", new { CustId = custId, DepositId = depositId, Siteid = siteid }, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();

            return result;
        }
        public DepositReceipt GetDepositReceipt(int depositId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<DepositReceipt>("DepositReceipt_Get", new { DepositId = depositId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<DepositLimitInfo> GetDepositLimitInfoList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositLimitInfo>("Member_DepositLimitInfo_Get", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<DepositMessage> GetDepositMessage(int depositId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositMessage>("Member_DepositMessage_Get", new { _DepositId = depositId }, commandType: CommandType.StoredProcedure);
        }
    }
}
