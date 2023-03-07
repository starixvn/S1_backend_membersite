using MemberSite.Service.Model;
using MemberSite.Service.Library;
using System.Data;
using Dapper;
using MemberSite.Service.BLL;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class DepositService
    {
        private readonly Common _common;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ShieldBLL _shieldBll;

        public DepositService(IServiceProvider provider)
        {
            _common = provider.GetService<Common>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _shieldBll = provider.GetRequiredService<ShieldBLL>();
        }
        
        public int DepositReq(DepositDetailInfo depositDetailInfo, out string transId, out string postScript)
        {
            transId = null;
            postScript = null;
            
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var p = new DynamicParameters(new { depositDetailInfo.DepositType, depositDetailInfo.CustId, depositDetailInfo.UserName, depositDetailInfo.CurrencyId, depositDetailInfo.SiteId, depositDetailInfo.DepositInfo, depositDetailInfo.Amount, depositDetailInfo.Memo, Prefix = $"D{_common.GetEnv()}" });
            p.Add("vPostScript", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            var result = _mainDbClient.Execute("DepositInfo_Ins", trans, p);
            trans.Commit();
            
            if (depositDetailInfo.SiteId != (int)SiteId.Nova88)
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
            
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var p = new DynamicParameters(new { depositDetailInfo.DepositType, depositDetailInfo.CustId, depositDetailInfo.UserName, depositDetailInfo.CurrencyId, depositDetailInfo.SiteId, depositFrom, depositDetailInfo.DepositInfo, depositDetailInfo.Amount, depositDetailInfo.Memo, Prefix = $"D{_common.GetEnv()}" });
            p.Add("vPostScript", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            var result = _mainDbClient.Execute("DepositInfoWithFrom_Ins", trans ,p);
            trans.Commit();
            
            if (depositDetailInfo.SiteId != (int)SiteId.Nova88)
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
            return _reportDbClient.Query<DepositInfoHistory>("DepositInfoRecent_Get_V3", new { CustId = custId, SiteId = siteId, TopNum = topNum });
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var p = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate, SiteId = siteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var depositInfoHistoryResult = _reportDbClient.Query<DepositInfoHistory>("DepositInfoSearch_Get_V3", p);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return depositInfoHistoryResult;
        }
       
        public int SetDepositReceipt(int custId, int depositId, string refNo, long depositTime)
        {
            return _mainDbClient.Execute("DepositReceipt_Set", new { CustId = custId, DepositId = depositId, RefNo = refNo, DepositTime = depositTime });
        }

        public async Task<int> SetDepositReceiptPic(int custId, int depositId, string picFileName, byte[] depositPic)
        {
            var p = new DynamicParameters(new
            { CustId = custId, DepositId = depositId, PicFileName = picFileName, DepositPic = depositPic });
            return await _mainDbClient.ExecuteAsync("DepositReceiptPic_Set", p);
        }
        
        public int SetDepositCancel(int custId, int depositId, int siteId)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = _mainDbClient.Execute("DepositInfoCancel_Set", trans, new { CustId = custId, DepositId = depositId, Siteid = siteId });
            trans.Commit();

            return result;
        }
        
        public DepositReceipt GetDepositReceipt(int depositId, int siteId)
        {
            return _reportDbClient.QueryFirst<DepositReceipt>("DepositReceipt_Get", new { DepositId = depositId, SiteId = siteId });
        }
        
        public IEnumerable<DepositLimitInfo> GetDepositLimitInfoList()
        {
            return _reportDbClient.Query<DepositLimitInfo>("Member_DepositLimitInfo_Get");
        }
    }
}
