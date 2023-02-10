using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using System.Data;
using System.Text.RegularExpressions;
using MemberSite.Service.BLL;

namespace MemberSite.Service.DAL
{
    public class WithdrawalService
    {
        private readonly Common common;
        private readonly WalletService walletService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ShieldBLL _shieldBll;
        private readonly int typePosition = 1000;

        public WithdrawalService(IServiceProvider provider)
        {
            common = provider.GetRequiredService<Common>();
            walletService = provider.GetRequiredService<WalletService>();
            _shieldBll = provider.GetRequiredService<ShieldBLL>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public int WithdrawalReq(int custId, string userName, int currencyId, int siteId, decimal amount, string memo, WithdrawalInfo withdrawalInfo, out decimal afterAmount, out string transId)
        {
            transId = "";
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalInfo), Prefix = $"W{common.GetEnv()}" });
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("ToKyc", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            _mainDbClient.Execute("Member_WithdrawalInfo_Ins", trans, p);
            transId = p.Get<string>("vTransId");

            if(string.IsNullOrEmpty(transId))
            {
                conn.Close();
                conn.Dispose();
                return 3; //Insert Fail
            }
       
            if (GetWithdrawalAvailableBalance(custId, trans) < amount)
            {
                conn.Close();
                conn.Dispose();
                return -3;
            }

            var result = -99;
            result = walletService.SetBalance(custId, siteId, userName, amount * -1, typePosition + 1, null, transId, out afterAmount, out _, trans);
            
            if (result > 0)
            {
                trans.Commit();

                if (siteId == (int) SiteId.Goal123)
                {
                    _ = Task.Run(async () =>
                    {
                        var checkResult = _shieldBll.CheckCustShieldRisk(
                            new Customer()
                            {
                                SiteId = siteId,
                                CurrencyId = currencyId,
                                CustId = custId
                            },
                            ShieldRiskTriggeredPoint.Withdrawal,
                            ShieldRiskTaggedIssue.HighRisk);
                    });
                }
                
                if ((p.Get<byte?>("ToKyc") ?? 0) == 1)
                {
                    conn.Close();
                    conn.Dispose();
                    return 2;
                }
                else
                {
                    conn.Close();
                    conn.Dispose();
                    return 0;
                }
            }
            else
            {
                conn.Close();
                conn.Dispose();
                return result;
            }
        }

        public int WithdrawalCryptoReq(int custId, string userName, int currencyId, int siteId, decimal amount, string memo, WithdrawalCryptoInfo withdrawalCryptoInfo, out decimal afterAmount, out string transId)
        {
            transId = "";
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalCryptoInfo), Prefix = $"W{common.GetEnv()}" });
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("ToKyc", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            _mainDbClient.Execute("Member_WithdrawalInfo_Ins", trans , p );
            transId = p.Get<string>("vTransId");
           
            if(GetWithdrawalAvailableBalance(custId,trans) < amount)
            {
                conn.Close();
                conn.Dispose();
                return -3;
            }

            var result = -99;
            result = walletService.SetBalance(custId, siteId, userName, amount * -1, typePosition + 1, null, transId, out afterAmount, out _, trans);
            if (result > 0)
            {
                trans.Commit();
                
                if (siteId == (int) SiteId.Goal123)
                {
                    _ = Task.Run(async () =>
                    {
                        var checkResult = _shieldBll.CheckCustShieldRisk(
                            new Customer()
                            {
                                SiteId = siteId,
                                CurrencyId = currencyId,
                                CustId = custId
                            },
                            ShieldRiskTriggeredPoint.Withdrawal,
                            ShieldRiskTaggedIssue.HighRisk);
                    });
                }

                if ((p.Get<byte?>("ToKyc") ?? 0) == 1)
                {
                    conn.Close();
                    conn.Dispose();
                    return 2;
                }
                else
                {
                    conn.Close();
                    conn.Dispose();
                    return 0;
                }
            }
            else
            {
                conn.Close();
                conn.Dispose();
                return result;
            }
        }
       
        public int WithdrawalEWalletReq(int custId, string userName, int currencyId, int siteId, decimal amount, string memo, WithdrawalEWalletInfo withdrawalEWalletInfo, out decimal afterAmount, out string transId)
        {
            transId = "";
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalEWalletInfo), Prefix = $"W{common.GetEnv()}" });
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("ToKyc", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            _mainDbClient.Execute("Member_WithdrawalInfo_Ins", trans ,p);
            transId = p.Get<string>("vTransId");

            if (GetWithdrawalAvailableBalance(custId,trans) < amount)
            {
                conn.Close();
                conn.Dispose();
                return -3;
            }

            var result = -99;
            result = walletService.SetBalance(custId, siteId, userName, amount * -1, typePosition + 1, null, transId, out afterAmount, out _, trans);
            if (result > 0)
            {
                trans.Commit();

                if ((p.Get<byte?>("ToKyc") ?? 0) == 1)
                {
                    conn.Close();
                    conn.Dispose();
                    return 2;
                }
                else
                {
                    conn.Close();
                    conn.Dispose();
                    return 0;
                }
            }
            else
            {
                conn.Close();
                conn.Dispose();
                return result;
            }
        }
       
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoRecent(int custId, int siteId, int topNum)
        {
            return _reportDbClient.Query<WithdrawalHistory>("WithdrawalInfoRecent_Get_V3", new { CustId = custId, SiteId = siteId, TopNum = topNum });
        }
        
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var p = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate, SiteId = siteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var withdrawalInfoHistoryResult = _reportDbClient.Query<WithdrawalHistory>("WithdrawalInfoSearch_Get_V3", p);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return withdrawalInfoHistoryResult;
        }

        public int SetWithdrawalCancel(int custId, string transId, int siteId, IEnumerable<int> forStatus, WithdrawalStatus status)
        {
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var withdrawalId);
            var result = 0;
            var type = 1003;  /*Cancel  Withdrawal 1003*/
            string UserName = null;
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                
                var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = "Customer Cancelled", ForStatus = sForStatus, Status = (int)status, WithdrawalInfo = (string)null, WithdrawalType = (int?)null, RefId = (string)null });
                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Member_WithdrawalInfoV2_Set",trans , p );
                custId = p.Get<int?>("CustId") ?? 0;
                UserName = p.Get<string>("UserName");
                var amount = p.Get<decimal>("Amount");
                
                if (result > 0)
                {
                    SetWithdrawalLog(withdrawalId, siteId, "Customer Cancelled", (int)status, UserName);

                    if (status == WithdrawalStatus.Cancelled)
                    {
                        result = walletService.SetBalance(custId, siteId, UserName, amount, type, $"Withdrawal Canceled: {amount}", transId, out _, out _, trans);
                    }
                            
                    if (result > 0)
                    {
                        trans.Commit();
                        //result = p.Get<int?>("CustId") ?? result;
                    }
                }
                conn.Close();
                conn.Dispose();
            }
            catch (Exception ex)
            {
                SetWithdrawalLog(withdrawalId, siteId, ex.Message, (int)WithdrawalStatus.Error, UserName);
            }
            return result;
        }

        private int SetWithdrawalLog(int withdrawalId, int siteId, string comment, int status, string Operator)
        {
            return _mainDbClient.Execute("WithdrawalLog_Ins", new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = comment, Action = status, Operator });
        }
       
        public decimal GetWithdrawalAvailableBalance(int custId)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var result = GetWithdrawalAvailableBalance(custId, trans);
            trans.Rollback();
            conn.Close();
            conn.Dispose();
            
            return result;
        }
      
        public decimal GetWithdrawalAvailableBalance(int custId, IDbTransaction trans)
        {
            return _reportDbClient.ExecuteScalar<decimal>("Common_WithdrawalAvailableBalance_Get", trans ,new { CustId = custId });
        }

        public WithdrawalLimitInfo GetCustWithdrawalLimitInfo(int custId)
        {
            return _reportDbClient.Query<WithdrawalLimitInfo>("Member_CustWithdrawalLimit_Get", new { custId }).FirstOrDefault();
        }

        public WithdrawalLimitInfoOutput GetWithdrawalLimitInfo(WithdrawalLimitInfoInput input)
        {
            return _mainDbClient.Query<WithdrawalLimitInfoOutput>("Member_WithdrawalLimitInfo_Get",
                new {input.SiteId, input.CurrencyId, input.VipLevel}).FirstOrDefault();
        }

        public DailyCustomerWithdrawalInfoOutput GetDailyCustomerWithdrawalInfo(DailyCustomerWithdrawalInfoInput input)
        {
            return _mainDbClient.Query<DailyCustomerWithdrawalInfoOutput>("Member_DailyCustomerWithdrawalInfo_Get",
                new {input.CustId}).FirstOrDefault();
        }
        
        public CustomerWithdrawalInfoOutput GetWithdrawalInfoCheck(CustomerWithdrawalInfoInput input)
        {
            return _mainDbClient.QueryFirst<CustomerWithdrawalInfoOutput>("Member_WithdrawalInfoCheck_Get",
                new DynamicParameters(new {input.CustId}));
        }

        public IEnumerable<USDTWithdrawalLimitInfoOutput> GetUSDTWithdrawalLimitInfoV2(USDTWithdrawalLimitInfoInput input)
        {
            return _reportDbClient.Query<USDTWithdrawalLimitInfoOutput>("Member_USDTWithdrawalLimitInfo_Get_V2",
                new
                {
                    _SiteId = input.SiteId,
                    _CurrencyId = input.CurrencyId,
                    _VipLevel = input.VipLevel,
                    _CryptoCurrencyId = input.CryptoCurrencyId,
                    _Network = input.Network
                });
        }

        public EWalletWithdrawalLimitInfoOutput GetEWalletWithdrawalLimitInfo(WithdrawalLimitInfoInput input)
        {
            return _reportDbClient.Query<EWalletWithdrawalLimitInfoOutput>("Member_EWalletWithdrawalLimitInfo_Get",
                new
                {
                    SiteId = input.SiteId,
                    CurrencyId = input.CurrencyId
                }).FirstOrDefault();
        }      
    }
}
