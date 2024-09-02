using System.Data;

using Dapper;

using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

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

        public int DepositReq(DepositDetailInfo depositDetailInfo, out string transId, out string postScript, string depositFrom = null)
        {
            transId = null;
            postScript = null;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(new
            {
                depositDetailInfo.DepositType,
                depositDetailInfo.CustId,
                depositDetailInfo.UserName,
                depositDetailInfo.CurrencyId,
                depositDetailInfo.SiteId,
                depositDetailInfo.DepositInfo,
                DepositFrom = depositFrom,
                depositDetailInfo.Amount,
                depositDetailInfo.Memo,
                Prefix = $"D{_common.GetEnv()}"
            });

            p.Add("vPostScript", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);

            var result = _mainDbClient.Execute("DepositInfoWithFrom_Ins", trans, p);
            trans.Commit();

            if (depositDetailInfo.SiteId is 2 or 3) // goal and bwing
            {
                _ = Task.Run(() =>
                {
                    _ = _shieldBll.CheckCustShieldRisk(
                        new Customer
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
            return _reportDbClient.Query<DepositInfoHistory>("DepositInfoRecent_Get_V4", new { CustId = custId, SiteId = siteId, TopNum = topNum });
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var p = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate, SiteId = siteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var depositInfoHistoryResult = _reportDbClient.Query<DepositInfoHistory>("DepositInfoSearch_Get_V4", p);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return depositInfoHistoryResult;
        }

        public int SetDepositReceipt(int custId, int depositId, string refNo, long depositTime)
        {
            return _mainDbClient.Execute("DepositReceipt_Set_V2", new { CustId = custId, DepositId = depositId, RefNo = refNo, DepositTime = depositTime });
        }

        public async Task<int> SetDepositReceiptPic(int custId, int depositId, int receiptNo, string picFileName, string userName)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId,
                DepositId = depositId,
                PicFileName = picFileName,
                ReceiptNo = receiptNo,
                Creator = userName,
                CreateFrom = 1
            });
            return await _mainDbClient.ExecuteAsync("DepositReceiptPic_Set_V3", p);
        }

        public int SetDepositCancel(string username, int depositId, int siteId)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            
            try
            {
                var p = new DynamicParameters(new
                {
                    _DepositId = depositId,
                    _SiteId = siteId,
                    _ActualDeposit = 0,
                    _ForStatus = JsonConvert.SerializeObject(new []{DepositStatusEnum.Pending}),
                    _Status = (int)DepositStatusEnum.Canceled,
                    _RefId = (string)null,
                    _ThirdId = (string)null,
                    _Operator = username
                });
                p.Add("_CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("_UserName", dbType: DbType.String, direction: ParameterDirection.Output);

                result = _mainDbClient.Execute("Service_DepositInfo_Set_V4", trans, p);

                if (result > 0)
                {
                    result = SetDepositLog(depositId, siteId, 
                        "Customer Cancelled", DepositStatusEnum.Canceled, 0,
                        username, trans);
                    
                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                SetDepositLog(depositId, siteId, ex.Message, DepositStatusEnum.Error, 0, username, trans);
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }
            
            return result;
        }
        
        private int SetDepositLog(int depositId, int siteId, string comment, DepositStatusEnum status, decimal actualDeposit, string @operator, IDbTransaction trans)
        {
            return _mainDbClient.Execute("DepositLog_Ins", trans,
                new DynamicParameters(new
                {
                    DepositId = depositId,
                    SiteId = siteId,
                    Comment = comment,
                    Action = (int)status,
                    ActualDeposit = actualDeposit,
                    Operator = @operator
                }));
        }

        public IEnumerable<DepositReceipt> GetDepositReceipt(int depositId, int siteId)
        {
            return _reportDbClient.Query<DepositReceipt>("DepositReceipt_Get_V2", new { DepositId = depositId, SiteId = siteId });
        }

        public IEnumerable<DepositLimitInfo> GetDepositLimitInfoList()
        {
            return _reportDbClient.Query<DepositLimitInfo>("Member_DepositLimitInfo_Get");
        }

        public ActualDepositModel GetActualDepositData(int siteId, int custId, DateTime startTime, DateTime endTime)
        {
            return _reportDbClient.Query<ActualDepositModel>("Member_ActualDeposit_Get", new { CustId = custId, SiteId = siteId, StartTime = startTime, EndTime = endTime }).FirstOrDefault();
        }
    }
}
