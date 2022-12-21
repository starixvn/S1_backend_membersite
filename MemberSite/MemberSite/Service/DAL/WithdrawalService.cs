using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.RegularExpressions;
using MemberSite.Service.BLL;

namespace MemberSite.Service.DAL
{
    public class WithdrawalService
    {
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly WalletService walletService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ShieldBLL _shieldBll;
        private readonly int typePosition = 1000;

        public WithdrawalService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
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
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalInfo), Prefix = $"W{common.GetEnv()}" });
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("ToKyc", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            conn.Execute("Member_WithdrawalInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            transId = p.Get<string>("vTransId");

            if(string.IsNullOrEmpty(transId))
            {
                return 3; //Insert Fail
            }
       
            if (GetWithdrawalAvailableBalance(custId, conn, trans) < amount)
            {
                return -3;
            }

            var result = -99;
            result = walletService.SetBalance(custId, siteId, userName, amount * -1, typePosition + 1, null, transId, out afterAmount, out _, conn, trans);
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
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return result;
            }
        }

        public int WithdrawalCryptoReq(int custId, string userName, int currencyId, int siteId, decimal amount, string memo, WithdrawalCryptoInfo withdrawalCryptoInfo, out decimal afterAmount, out string transId)
        {
            transId = "";
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalCryptoInfo), Prefix = $"W{common.GetEnv()}" });
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("ToKyc", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            conn.Execute("Member_WithdrawalInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            transId = p.Get<string>("vTransId");
           
            if(GetWithdrawalAvailableBalance(custId, conn, trans) < amount)
            {
                return -3;
            }

            var result = -99;
            result = walletService.SetBalance(custId, siteId, userName, amount * -1, typePosition + 1, null, transId, out afterAmount, out _, conn, trans);
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
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return result;
            }
        }
        public int WithdrawalEWalletReq(int custId, string userName, int currencyId, int siteId, decimal amount, string memo, WithdrawalEWalletInfo withdrawalEWalletInfo, out decimal afterAmount, out string transId)
        {
            transId = "";
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Amount = amount, Memo = memo, WithdrawalInfo = Newtonsoft.Json.JsonConvert.SerializeObject(withdrawalEWalletInfo), Prefix = $"W{common.GetEnv()}" });
            p.Add("vTransId", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("ToKyc", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            conn.Execute("Member_WithdrawalInfo_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            transId = p.Get<string>("vTransId");

            if (GetWithdrawalAvailableBalance(custId, conn, trans) < amount)
            {
                return -3;
            }

            var result = -99;
            result = walletService.SetBalance(custId, siteId, userName, amount * -1, typePosition + 1, null, transId, out afterAmount, out _, conn, trans);
            if (result > 0)
            {
                trans.Commit();

                if ((p.Get<byte?>("ToKyc") ?? 0) == 1)
                {
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return result;
            }
        }
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoRecent(int custId, int siteId, int topNum)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<WithdrawalHistory>("WithdrawalInfoRecent_Get_V3", new { CustId = custId, SiteId = siteId, TopNum = topNum }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate, SiteId = siteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var withdrawalInfoHistoryResult = conn.Query<WithdrawalHistory>("WithdrawalInfoSearch_Get_V3", p, commandType: CommandType.StoredProcedure);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return withdrawalInfoHistoryResult;
        }

        public int SetWithdrawalCancel(int custId, string transId, int siteId, IEnumerable<int> forStatus, WithdrawalStatus status)
        {
            var withdrawalId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out withdrawalId);
            var result = 0;
            var type = 1003;  /*Cancel  Withdrawal 1003*/
            string UserName = null;
            try
            {
                using (var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                        var p = new DynamicParameters(new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = "Customer Cancelled", ForStatus = sForStatus, Status = (int)status, WithdrawalInfo = (string)null, WithdrawalType = (int?)null, RefId = (string)null });
                        p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                        p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                        result = conn.Execute("Member_WithdrawalInfoV2_Set", p, trans, commandType: CommandType.StoredProcedure);
                        custId = p.Get<int?>("CustId") ?? 0;
                        UserName = p.Get<string>("UserName");
                        var amount = p.Get<decimal>("Amount");
                        if (result > 0)
                        {
                            SetWithdrawalLog(withdrawalId, siteId, "Customer Cancelled", (int)status, UserName);

                            if (status == WithdrawalStatus.Cancelled)
                            {
                                result = walletService.SetBalance(custId, siteId, UserName, amount, type, $"Withdrawal Canceled: {amount}", transId, out _, out _, conn, trans);
                            }
                            
                            if (result > 0)
                            {
                                trans.Commit();
                                //result = p.Get<int?>("CustId") ?? result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetWithdrawalLog(withdrawalId, siteId, ex.Message, (int)WithdrawalStatus.Error, UserName);
            }

            return result;
        }

        private int SetWithdrawalLog(int withdrawalId, int siteId, string comment, int status, string Operator)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("WithdrawalLog_Ins", new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = comment, Action = status, Operator }, commandType: CommandType.StoredProcedure);
        }
        public decimal GetWithdrawalAvailableBalance(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction(IsolationLevel.ReadUncommitted);
            var result = GetWithdrawalAvailableBalance(custId, conn, trans);
            trans.Rollback();
            return result;
        }
        public decimal GetWithdrawalAvailableBalance(int custId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.ExecuteScalar<decimal>("Common_WithdrawalAvailableBalance_Get", new { CustId = custId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }

        public WithdrawalLimitInfo GetCustWithdrawalLimitInfo(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<WithdrawalLimitInfo>("Member_CustWithdrawalLimit_Get", new { custId }, commandType: CommandType.StoredProcedure).FirstOrDefault();
        }

        public WithdrawalLimitInfoOutput GetWithdrawalLimitInfo(WithdrawalLimitInfoInput input)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<WithdrawalLimitInfoOutput>("Member_WithdrawalLimitInfo_Get",
                new {input.SiteId, input.CurrencyId, input.VipLevel}, commandType: CommandType.StoredProcedure).FirstOrDefault();
        }

        public DailyCustomerWithdrawalInfoOutput GetDailyCustomerWithdrawalInfo(DailyCustomerWithdrawalInfoInput input)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<DailyCustomerWithdrawalInfoOutput>("Member_DailyCustomerWithdrawalInfo_Get",
                new {input.CustId}, commandType: CommandType.StoredProcedure).FirstOrDefault();
        }
        
        public CustomerWithdrawalInfoOutput GetWithdrawalInfoCheck(CustomerWithdrawalInfoInput input)
        {
            return _mainDbClient.QueryFirst<CustomerWithdrawalInfoOutput>("Member_WithdrawalInfoCheck_Get",
                new DynamicParameters(new {input.CustId}));
        }

        public IEnumerable<USDTWithdrawalLimitInfoOutput> GetUSDTWithdrawalLimitInfoV2(USDTWithdrawalLimitInfoInput input)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<USDTWithdrawalLimitInfoOutput>("Member_USDTWithdrawalLimitInfo_Get_V2",
                new
                {
                    _SiteId = input.SiteId,
                    _CurrencyId = input.CurrencyId,
                    _VipLevel = input.VipLevel,
                    _CryptoCurrencyId = input.CryptoCurrencyId,
                    _Network = input.Network
                }, commandType: CommandType.StoredProcedure);
        }

        public EWalletWithdrawalLimitInfoOutput GetEWalletWithdrawalLimitInfo(WithdrawalLimitInfoInput input)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<EWalletWithdrawalLimitInfoOutput>("Member_EWalletWithdrawalLimitInfo_Get",
                new
                {
                    SiteId = input.SiteId,
                    CurrencyId = input.CurrencyId
                }, commandType: CommandType.StoredProcedure).FirstOrDefault();
        }      
    }
}
