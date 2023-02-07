using MemberSite.Extensions;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Interfaces;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace MemberSite.Service.BLL
{
    public class PaymentBLL
    {
        private readonly BankInfoOffBLL bankInfoOffBLL;
        private readonly CustInfoBll custInfoBLL;
        private readonly DepositService _depositService;
        private readonly WithdrawalService withdrawalService;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly StatementService statementService;
        private readonly SelLangBLL selLangBLL;
        private readonly IReadOnlyDictionary<string, string> singleWalletType;
        private readonly PartitionService _partitionService;
        private readonly ICacheHelper _cacheHelper;
        private readonly IReadOnlyDictionary<int, string> _tpList;
        private readonly IReadOnlyDictionary<int, string> _gameTypeList;
        private readonly ILogger<PaymentBLL> _logger;

        public PaymentBLL(IServiceProvider provider)
        {
            bankInfoOffBLL = provider.GetRequiredService<BankInfoOffBLL>();
            custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            _depositService = provider.GetRequiredService<DepositService>();
            withdrawalService = provider.GetRequiredService<WithdrawalService>();
            bankInfoBLL = provider.GetRequiredService<BankInfoBLL>();
            statementService = provider.GetRequiredService<StatementService>();
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
          
            singleWalletType = new Dictionary<string, string>
            {
                {"1", "lbl_Stake"  },
                {"2", "lbl_Refund" },
                {"3", "lbl_Settled"},
                {"4", "lbl_Prize"  },
                {"5", "lbl_UnSettle"}
            };
            _partitionService = provider.GetRequiredService<PartitionService>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();

            _tpList = statementService.GetTpListWithCache().ToDictionary(i => i.Id , i =>i.TpName);
            _gameTypeList = statementService.GetGameTypeListWithCache().ToDictionary(i => i.Type, i => i.TypeName);
            _logger = provider.GetRequiredService<ILogger<PaymentBLL>>();
        }

        /// <summary>
        /// Deposit for Local Bank
        /// </summary>
        /// <param name="custId">user id</param>
        /// <param name="sysId">bank id</param>
        /// <param name="amount">trans amount</param>
        /// <param name="memo">meno</param>
        /// <param name="depositRep3">output</param>
        /// <returns></returns>
        public int DepositReqLocalBank(int custId, int sysId, decimal amount, string memo, out DepositRep3 depositRep3)
        {
            depositRep3 = new DepositRep3();

            if (amount <= 0)
                return -1;
            
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var pointLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var localBankList = bankInfoOffBLL.GetOfflineBankAccount(custInfo.SiteId)
                .Where(x => x.CurrencyId == custInfo.CurrencyId
                            && (custInfo.UserLevel > pointLevel || pointLevel == 8 || x.PointLevels.Contains(pointLevel))
                            && x.VipLevels.Contains(custInfo.UserLevel)
                            && x.SysId == sysId)
                .ToList();
            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var depositLimitInfo = depositLimitInfoList?.FirstOrDefault(x =>
                x.SiteId == custInfo.SiteId && x.VipLevel == pointLevel && x.DepositType == (int)DepositType.LocalBank &&
                x.CurrencyId == custInfo.CurrencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -4;
            }

            if (depositLimitInfo != null)
            {
                foreach (var localBack in localBankList)
                {
                    FixDepositLimit(localBack, depositLimitInfo);
                }
            }

            var info = localBankList
                .Where(x => (x.MinAmount == 0 && x.MaxAmount == 0) || (amount >= x.MinAmount && amount <= x.MaxAmount))
                .OrderBy(x => x.BSort)
                .Select(x => new
                {
                    x.SysId,
                    x.BankId,
                    x.BankLogo,
                    x.BankName,
                    x.BankSite,
                    x.AccountName,
                    x.AccountNo,
                    IFSCCode = x.AccountCode
                }).FirstOrDefault();
                
            if (info != null)
            {
                var depositDetailInfo = new DepositDetailInfo
                {
                    Amount = amount,
                    CustId = custId,
                    DepositType = (int) DepositType.LocalBank,
                    Memo = memo,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId
                };

                var depositInfo3_1 = new DepositInfo3_1
                {
                    AccountName = info.AccountName,
                    CurrencyId = custInfo.CurrencyId,
                    SysId = info.SysId,
                    AccountNo = info.AccountNo,
                    IFSCCode = info.IFSCCode,
                    Ver = 1
                };
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositInfo3_1);

                _depositService.DepositReq(depositDetailInfo, out var transId, out var postScript);
                depositRep3.TransId = transId;
                depositRep3.PostScript = postScript;
                depositRep3.AccountNo = depositInfo3_1.AccountNo;
                depositRep3.AccountName = depositInfo3_1.AccountName;
                depositRep3.IFSCCode = depositInfo3_1.IFSCCode;
                depositRep3.BankName = info.BankName;
                depositRep3.BankLogo = info.BankLogo;
                depositRep3.BankSite = info.BankSite;

                return 0;
            }
            
            var tmpInfo = localBankList.
                Select(x => new
                {
                    x.SysId,
                    x.BankId,
                    x.BankLogo,
                    x.BankName,
                    x.BankSite,
                    x.BSort,
                    x.AccountName,
                    x.AccountNo,
                    IFSCCode = x.AccountCode,
                    x.MinAmount,
                    x.MaxAmount
                })
                .OrderByDescending(x => x.MaxAmount)
                .ThenBy(x => x.MinAmount)
                .ThenBy(x => x.BSort)
                .FirstOrDefault();

            if (info == null && tmpInfo == null)
            {
                return -2;
            }

            if (info == null && tmpInfo != null)
            {
                depositRep3.MaxAmount = tmpInfo.MaxAmount;
                depositRep3.MinAmount = tmpInfo.MinAmount;

                return -3;
            }

            return 0;
        }

        public int DepositReqLocalBankWithFrom(int custId, int sysId, decimal amount, string memo, string depositFromInfo, out DepositRep3 depositRep3)
        {
            depositRep3 = new DepositRep3();

            if (amount <= 0)
                return -1;
            
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var pointLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var localBankList = bankInfoOffBLL.GetOfflineBankAccount(custInfo.SiteId)
                .Where(x => x.CurrencyId == custInfo.CurrencyId
                            && (custInfo.UserLevel > pointLevel || pointLevel == 8 || x.PointLevels.Contains(pointLevel))
                            && x.VipLevels.Contains(custInfo.UserLevel)
                            && x.SysId == sysId)
                .ToList();
            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == pointLevel && x.DepositType == (int)DepositType.LocalBank && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -4;
            }

            if (depositLimitInfo != null && localBankList != null)
            {
                foreach (var localBack in localBankList)
                {
                    FixDepositLimit(localBack, depositLimitInfo);
                }
            }

            var info = localBankList
                .Where(x => (x.MinAmount == 0 && x.MaxAmount == 0) || (amount >= x.MinAmount && amount <= x.MaxAmount))
                .OrderBy(x => x.BSort)
                .Select(x => new
                {
                    x.SysId,
                    x.BankId,
                    x.BankLogo,
                    x.BankName,
                    x.BankSite,
                    x.AccountName,
                    x.AccountNo,
                    IFSCCode = x.AccountCode
                }).FirstOrDefault();

            if (info != null)
            {
                var depositDetailInfo = new DepositDetailInfo
                {
                    Amount = amount,
                    CustId = custId,
                    DepositType = (int)DepositType.LocalBank,
                    Memo = memo,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId
                };

                var depositInfo3_1 = new DepositInfo3_1
                {
                    AccountName = info.AccountName,
                    CurrencyId = custInfo.CurrencyId,
                    SysId = info.SysId,
                    AccountNo = info.AccountNo,
                    IFSCCode = info.IFSCCode,
                    Ver = 1
                };
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositInfo3_1);
                
                _depositService.DepositReqwithFrom(depositDetailInfo, depositFromInfo, out var transId, out var postScript);

                depositRep3.TransId = transId;
                depositRep3.PostScript = postScript;
                depositRep3.AccountNo = depositInfo3_1.AccountNo;
                depositRep3.AccountName = depositInfo3_1.AccountName;
                depositRep3.IFSCCode = depositInfo3_1.IFSCCode;
                depositRep3.BankName = info.BankName;
                depositRep3.BankLogo = info.BankLogo;
                depositRep3.BankSite = info.BankSite;

                return 0;
            }

            var tmpInfo = localBankList.
                Select(x => new
                {
                    x.SysId,
                    x.BankId,
                    x.BankLogo,
                    x.BankName,
                    x.BankSite,
                    x.BSort,
                    x.AccountName,
                    x.AccountNo,
                    IFSCCode = x.AccountCode,
                    x.MinAmount,
                    x.MaxAmount
                })
                .OrderByDescending(x => x.MaxAmount)
                .ThenBy(x => x.MinAmount)
                .ThenBy(x => x.BSort)
                .FirstOrDefault();

            if (info == null && tmpInfo == null)
            {
                return -2;
            }

            if (info == null && tmpInfo != null)
            {
                depositRep3.MaxAmount = tmpInfo.MaxAmount;
                depositRep3.MinAmount = tmpInfo.MinAmount;

                return -3;
            }

            return 0;
        }

        public int DepositReqOnlineBanking(int custId, int sysId, int payId, int channelId, decimal amount, out string transId, out BankInfoPay onlineBank, DepositType depositType = DepositType.OnlineBanking)
        {
            transId = null;
            onlineBank = null;
            if (amount <= 0)
                return -1;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            
            
            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var pointLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var onlineBankList = depositType == DepositType.OnlineBanking ? 
                bankInfoBLL.GetOnlineBankList(custInfo.SiteId) : 
                bankInfoBLL.GetAutoChannelBankInfoList(custInfo.SiteId, depositType);
            
            _logger.LogInformation("onlineBankList: " + JsonConvert.SerializeObject(onlineBankList));
            
            onlineBank = onlineBankList
                .FirstOrDefault(x => pointLevel >= x.PointLevel
                                     && x.ChannelId == channelId
                                     && (x.SysId == sysId || !x.SelBank)
                                     && x.PayId == payId
                                     && (x.CurrencyId == custInfo.CurrencyId || !x.SelBank));

            
            
            _logger.LogInformation("onlineBank: " + JsonConvert.SerializeObject(onlineBank));
            
            var depositLimitInfo = depositLimitInfoList?
                .FirstOrDefault(x => x.SiteId == custInfo.SiteId
                                     && x.VipLevel == pointLevel
                                     && x.DepositType == (int)depositType
                                     && x.CurrencyId == custInfo.CurrencyId);
            
            _logger.LogInformation("depositLimitInfo: " + JsonConvert.SerializeObject(depositLimitInfo));


            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            if (depositLimitInfo != null && onlineBank != null)
            {
                FixDepositLimit(onlineBank, depositLimitInfo);
            }
            _logger.LogInformation("after FixDepositLimit");

            if (onlineBank == null || onlineBank.MaxAmount < amount || onlineBank.MinAmount > amount)
            {
                return -2;
            }

            if (sysId == 0)
            {
                onlineBank.SelBank = false;
                onlineBank.SysId = 0;
                onlineBank.SysBankCode = "";
                onlineBank.SysBankName = "";
                onlineBank.BankLogo = "";
                onlineBank.BankName = "";
            }

            var depositDetailInfo = new DepositDetailInfo
            {
                Amount = amount,
                CustId = custId,
                DepositType = (int)depositType,
                Memo = null,
                SiteId = custInfo.SiteId,
                UserName = custInfo.UserName,
                CurrencyId = custInfo.CurrencyId,
                DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(onlineBank)
            };
            
            _logger.LogInformation("depositDetailInfo: "+ JsonConvert.SerializeObject(depositDetailInfo));

            _depositService.DepositReq(depositDetailInfo, out transId, out var postScript);
            _logger.LogInformation("transId: "+ transId);
            return 0;
        }
        public int WithdrawalReq(int custId, int sysId, string accountName, string accountNo, string iFSCCode, decimal amount, string memo, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalInfo = new WithdrawalInfo();
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (custInfoBLL.IsConvertRequired(CustInfo.CurrencyId))
            {
                checkAmount /= 1000;
            }

            if(Math.Ceiling(checkAmount) != Math.Floor(checkAmount))
            {
                return -1;
            }

            withdrawalInfo.AccountName = accountName;
            withdrawalInfo.AccountNo = accountNo;
            withdrawalInfo.SysId = sysId;
            withdrawalInfo.IFSCCode = iFSCCode;

            return withdrawalService.WithdrawalReq(custId, CustInfo.UserName, CustInfo.CurrencyId, CustInfo.SiteId, amount, memo, withdrawalInfo, out afterAmount, out transId);
        }
        public int WithdrawalCryptoReq(int custId, string accountName, USDTDetail cryptoDetail, string accountNo, decimal amount, string memo,
            decimal tmpRate, decimal tmpCryptoAmount, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalCryptoInfo = new WithdrawalCryptoInfo();
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (custInfoBLL.IsConvertRequired(CustInfo.CurrencyId))
            {
                checkAmount /= 1000;
            }

            if (Math.Ceiling(checkAmount) != Math.Floor(checkAmount))
            {
                return -1;
            }
            
            withdrawalCryptoInfo.AccountType = "crypto";
            withdrawalCryptoInfo.AccountName = accountName;
            withdrawalCryptoInfo.NickName = cryptoDetail.NickName;
            withdrawalCryptoInfo.AccountNo = accountNo;
            withdrawalCryptoInfo.Network = cryptoDetail.Network;
            withdrawalCryptoInfo.CurrencyId = cryptoDetail.CurrencyId;
            withdrawalCryptoInfo.TempExchangeAmount = tmpCryptoAmount;
            withdrawalCryptoInfo.TempExchangeRate = tmpRate;
            
            return withdrawalService.WithdrawalCryptoReq(custId, CustInfo.UserName, CustInfo.CurrencyId, CustInfo.SiteId, amount, memo, withdrawalCryptoInfo, out afterAmount, out transId);
        }

        public int WithdrawalEWalletReq(int custId, string accountName, int sysId, string accountNo, decimal amount, string memo, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalEWalletInfo = new WithdrawalEWalletInfo();
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (custInfoBLL.IsConvertRequired(CustInfo.CurrencyId))
            {
                checkAmount /= 1000;
            }

            if (Math.Ceiling(checkAmount) != Math.Floor(checkAmount))
            {
                return -1;
            }
            
            var eWalletInfoSysList = bankInfoBLL.GetEWalletInfoSysListByCurrency(CustInfo.CurrencyId).ToList();
            var eWalletInfoSys = eWalletInfoSysList.FirstOrDefault(x => x.SysId == sysId);
            withdrawalEWalletInfo.AccountName = accountName;
            withdrawalEWalletInfo.AccountNo = accountNo;
            withdrawalEWalletInfo.EWalletSysId = sysId;
            withdrawalEWalletInfo.EWalletName = eWalletInfoSys.EWalletName;
            withdrawalEWalletInfo.AccountType = "ewallet";
            return withdrawalService.WithdrawalEWalletReq(custId, CustInfo.UserName, CustInfo.CurrencyId, CustInfo.SiteId, amount, memo, withdrawalEWalletInfo, out afterAmount, out transId);
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoRecent(int custId)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var depositInfoRecent = _depositService.GetDepositInfoRecent(custId, CustInfo.SiteId, 5);
            foreach (var depositInfo in depositInfoRecent)
            {
                if (depositInfo.DepositType == (int)DepositType.LocalBank)
                {
                    depositInfo.DepositRep = Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo3_1>(depositInfo.DepositInfo);
                    var depositInfoRef = (DepositInfo3_1)depositInfo.DepositRep;
                    depositInfo.BankInfo = bankInfoBLL.GetBankInfoSysListByCurrency(CustInfo.CurrencyId).FirstOrDefault(x => x.SysId == depositInfoRef.SysId);
                }

                if ("Fake MobilePay".Equals(depositInfo.Memo, StringComparison.OrdinalIgnoreCase))
                {
                    depositInfo.DepositType = (int)DepositType.MobilePayment;
                }

                List<DepositMessageDetail> depositMessageDetails = new List<DepositMessageDetail>();
                if (depositInfo.DepositMessage != null)
                {
                    var depositMessage = JsonConvert.DeserializeObject<List<DepositMessage>>(depositInfo.DepositMessage);

                    depositMessage.ForEach(x =>
                    {
                        var offset = DateTime.Now.ToDateTimeOffset(depositInfo.CustTimeZone).Offset;
                        depositMessageDetails.Add(new DepositMessageDetail()
                        {
                            CreateTime = x.CreateTime.ToOffset(offset),
                            ErrorTypeName = selLangBLL.GetMsg($"msg_{x.Status.ToString()}"),
                            Message = selLangBLL.GetMsg($"msg_{x.Status.ToString()}_Detail"),
                        });
                    });
                }
                depositInfo.DepositMessageDetail = depositMessageDetails;
            }

            return depositInfoRecent;
        }

        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoRecent(int custId)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var withdrawalInfo = withdrawalService.GetWithdrawalInfoRecent(custId, CustInfo.SiteId, 5);
            return withdrawalInfo;
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoHistory(int custId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var depositInfoHistory = _depositService.GetDepositInfoHistory(CustInfo.CustId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);

            foreach (var depositInfo in depositInfoHistory)
            {
                if (depositInfo.DepositType == (int)DepositType.LocalBank)
                {
                    depositInfo.DepositRep = Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo3_1>(depositInfo.DepositInfo);
                    var depositInfoRef = (DepositInfo3_1)depositInfo.DepositRep;
                    depositInfo.BankInfo = bankInfoBLL.GetBankInfoSysListByCurrency(CustInfo.CurrencyId).FirstOrDefault(x => x.SysId == depositInfoRef.SysId);
                }
                List<DepositMessageDetail> depositMessageDetails = new List<DepositMessageDetail>();
                if (depositInfo.DepositMessage != null)
                {
                    var depositMessage = JsonConvert.DeserializeObject<List<DepositMessage>>(depositInfo.DepositMessage);

                    depositMessage.ForEach(x =>
                    {
                        var offset = DateTime.Now.ToDateTimeOffset(depositInfo.CustTimeZone).Offset;
                        depositMessageDetails.Add(new DepositMessageDetail()
                        {
                            CreateTime = x.CreateTime.ToOffset(offset),
                            ErrorTypeName = selLangBLL.GetMsg($"msg_{x.Status.ToString()}"),
                            Message = selLangBLL.GetMsg($"msg_{x.Status.ToString()}_Detail"),
                        });
                    });
                }
                depositInfo.DepositMessageDetail = depositMessageDetails;
            }
            return depositInfoHistory;
        }
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoHistory(int custId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var withdrawalInfoHistory = withdrawalService.GetWithdrawalInfoHistory(CustInfo.CustId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);
            return withdrawalInfoHistory;
        }

        public IEnumerable<Statement> GetStatement(int custId, long start, long end, int pageNumber, int pageSize, out IPageInfo pageInfo)
        {
            pageInfo = new PageInfo();
            var (startDate, endDate) = CheckTimeRange(start, end);
            var key = $"GetFinancialLog:{custId}:{startDate:yyyyMMddHHmmssfff}:{endDate:yyyyMMddHHmmssfff}";
            var cacheTime = TimeSpan.FromMinutes(1);

            if (_cacheHelper.KeyExists(key, RedisDataBase.Shared))
            {
                //Redis Pagination
                var total = (int)_cacheHelper.GetListLength(key, RedisDataBase.Shared);
                var (startIndex, endIndex) = pageInfo.RedisPagination(total, pageNumber, pageSize);
                var cacheList = _cacheHelper.GetListByRange<Statement>(key, startIndex, endIndex, RedisDataBase.Shared);
                foreach (var item in cacheList)
                {
                    item.TypeName = GetTransTypeNameV2(item.Type, item.TypeName);
                }
                return cacheList;
            }
            object RefTransId = null;
            //Search range table
            var parameters = new
            {
                CustId = custId,
                StartDate = startDate,
                EndDate = endDate,
                RefTransId,
            };
            var data = _partitionService.MonthlyQuery<Statement>(startDate, endDate, "Service_FinancialLog_Get", parameters);
            var list = data.OrderByDescending(i => i.CreateTime).ToList();

            // Add Cache by List      
            _ = Task.Run(() =>
            {
                _cacheHelper.InsertList(key, cacheTime, list, RedisDataBase.Shared);
            });


            var (index, count) = pageInfo.ListPagination(list.Count, pageNumber, pageSize);
            var datas = new List<Statement>();
            try
            {              
                datas = list.GetRange(index, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"list.Count = {list.Count}, pageNumber = {pageNumber}, pageSize = {pageSize}, index = {index}, count = {count}");
            }

            foreach (var item in datas)
            {
                item.TypeName = GetTransTypeNameV2(item.Type, item.TypeName);
            }
            return datas;
        }
        
        private string GetTransTypeNameV2(string Type, string TypeName)
        {
            if (Type.Length < 5)
            {
                return TypeName;
            }
            else
            {
                singleWalletType.TryGetValue(Type.Substring(1, 1), out var WalletType);
                int.TryParse(Type.Substring(2), out var tempId);
                string tpName = null;

                if (Type.StartsWith("1"))
                {
                    tpName = _tpList.GetValueOrDefault(tempId, string.Empty);
                }
                else if (Type.StartsWith("2"))
                {
                    tpName = _gameTypeList.GetValueOrDefault(tempId, string.Empty);
                }

                return $"{tpName} {selLangBLL.GetResourceValue(WalletType)}";
            }
        }

        public int SetDepositReceipt(int custId, string transId, string refNo, long depositTime)
        {
            var depositId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            return _depositService.SetDepositReceipt(custId, depositId, refNo, depositTime);
        }
        public async Task<int> SetDepositReceiptPic(int custId, string transId, string picFileName, byte[] depositPic)
        {
            var depositId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            return await _depositService.SetDepositReceiptPic(custId, depositId, picFileName, depositPic);
        }
        public int SetDepositCancel(int custId, string transId)
        {
            var depositId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            return _depositService.SetDepositCancel(CustInfo.CustId, depositId, CustInfo.SiteId);
        }

        public int SetWithdrawalCancel(int custId, string transId)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var forStatus = new List<int>
            {
                (int)WithdrawalStatus.Pending,
                (int)WithdrawalStatus.ToKyc
            };
            return withdrawalService.SetWithdrawalCancel(CustInfo.CustId, transId, CustInfo.SiteId, forStatus, WithdrawalStatus.Cancelled);
        }
        public DepositReceipt GetDepositReceipt(string transId, int custId)
        {
            var depositId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            return _depositService.GetDepositReceipt(depositId, CustInfo.SiteId);
        }

        public decimal GetWithdrawalAvailableBalance(int custId)
        {
            return withdrawalService.GetWithdrawalAvailableBalance(custId);
        }
        public WithdrawalLimitInfo GetCustWithdrawalLimitInfo(int custId)
        {
            return withdrawalService.GetCustWithdrawalLimitInfo(custId);
        }

        private static void FixDepositLimit(IPaymentLimit paymentLimit, DepositLimitInfo depositLimitInfo)
        {
            if ((paymentLimit.MaxAmount != 0 && depositLimitInfo.DepositMin > paymentLimit.MaxAmount)
                 || depositLimitInfo.DepositMax < paymentLimit.MinAmount)
            {
                paymentLimit.MaxAmount = -1;
            }
            else
            {
                if (paymentLimit.MaxAmount == 0 || (paymentLimit.MaxAmount > depositLimitInfo.DepositMax && depositLimitInfo.DepositMax != 0))
                {
                    paymentLimit.MaxAmount = depositLimitInfo.DepositMax;
                }

                if (paymentLimit.MinAmount == 0 || paymentLimit.MinAmount < depositLimitInfo.DepositMin)
                {
                    paymentLimit.MinAmount = depositLimitInfo.DepositMin;
                }

                if (paymentLimit.MinAmount > paymentLimit.MaxAmount)
                {
                    paymentLimit.MaxAmount = paymentLimit.MinAmount;
                }
            }
        }

        public IEnumerable<TpBetRecordListOneDay> GetCustTpBetRecordListOneDay(int custId, string selDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            return statementService.GetCustTpBetRecordListOneDay(custId, selDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
        }
        public IEnumerable<TpBetRecordbyCategoryId> GetCustTpBetRecordListbyCategoryId(int custId, string selDate, int categoryId, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            return statementService.GetCustTpBetRecordListbyCategoryId(custId, selDate, categoryId, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
        }
        public IEnumerable<TpBetRecordDetailbyGameType> GetCustTpBetRecordDetailListbyGameType(int custId, string selDate, int gameTypeId, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            return statementService.GetCustTpBetRecordDetailListbyGameType(custId, selDate, gameTypeId, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
        }
        public IEnumerable<TpBetRecordListDateRange> GetCustTpBetRecordDateRange(int custId, string startDate, string endDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            return statementService.GetCustTpBetRecordDateRange( custId, startDate, endDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
        }
        public WithdrawalLimitInfoOutput GetWithdrawalLimitInfo(WithdrawalLimitInfoInput input)
        {
            return withdrawalService.GetWithdrawalLimitInfo(input);
        }
        public DailyCustomerWithdrawalInfoOutput GetDailyCustomerWithdrawalInfo(DailyCustomerWithdrawalInfoInput input)
        {
            return withdrawalService.GetDailyCustomerWithdrawalInfo(input);
        }
        public CustomerWithdrawalInfoOutput GetWithdrawalInfoCheck(CustomerWithdrawalInfoInput input)
        {
            return withdrawalService.GetWithdrawalInfoCheck(input);
        }

        public CryptoInfoData GetSupportCryptoInfo(int siteId)
        {
            var infos = statementService.GetSupportCryptoInfo(siteId);
            var cryptoCurrency = infos
                .GroupBy(x => new { x.CurrencyId, x.CurrencyName})
                .Select(x => new CryptoCurrency
                {
                    CurrencyId = x.Key.CurrencyId,
                    CurrencyName = x.Key.CurrencyName
                });

            var cryptoNetwork = infos.Select(x => new CryptoNetwork
            {
                CurrencyId = x.CurrencyId,
                Network = x.Network
            });

            return new CryptoInfoData
            {
                CryptoCurrency = cryptoCurrency,
                CryptoNetwork = cryptoNetwork
            };
        }

        public IEnumerable<USDTWithdrawalLimitInfoOutput> GetUSDTWithdrawalLimitInfoV2(USDTWithdrawalLimitInfoInput input)
        {
            return withdrawalService.GetUSDTWithdrawalLimitInfoV2(input);
        }

        public EWalletWithdrawalLimitInfoOutput GetEWalletWithdrawalLimitInfo(WithdrawalLimitInfoInput input)
        {
            return withdrawalService.GetEWalletWithdrawalLimitInfo(input);
        }

        private (DateTime startDate, DateTime endDate) CheckTimeRange(long start, long end)
        {
            var startDate = DateTimeOffset.FromUnixTimeSeconds(start).DateTime;
            var endDate = DateTimeOffset.FromUnixTimeSeconds(end).DateTime;

            var oneYear = DateTime.UtcNow.AddYears(-1);//不可超過一年
            var tomorrow = DateTime.UtcNow.AddDays(1).AddMilliseconds(-1);//不可超過今天

            if (startDate < oneYear)
            {
                startDate = oneYear;
            }
            if (endDate < oneYear)
            {
                endDate = oneYear;
            }

            if (startDate > tomorrow)
            {
                startDate = tomorrow;
            }
            if (endDate > tomorrow)
            {
                endDate = tomorrow;
            }
            return (startDate, endDate);
        }

    }
}
