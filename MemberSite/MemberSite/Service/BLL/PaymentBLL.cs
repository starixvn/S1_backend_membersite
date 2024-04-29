using System.Net;
using System.Text.RegularExpressions;

using MemberSite.Exceptions;
using MemberSite.Extensions;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Interfaces;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class PaymentBLL
    {
        private readonly BankInfoOffBLL bankInfoOffBLL;
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositService _depositService;
        private readonly WithdrawalService withdrawalService;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly StatementService statementService;
        private readonly SelLangBLL selLangBLL;
        private readonly IReadOnlyDictionary<string, string> singleWalletType;
        private readonly PartitionService _partitionService;
        private readonly ShieldService _shieldService;
        private readonly ICacheHelper _cacheHelper;
        private readonly IReadOnlyDictionary<int, string> _tpList;
        private readonly IReadOnlyDictionary<int, string> _gameTypeList;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PaymentBLL> _logger;
        private readonly string _awsDepositUrl;
        private readonly IovationService _iovationService;
        private readonly string _backendUrl;
        private readonly CourierAdapter _courierAdapter;
        private readonly Tools _tools;

        public PaymentBLL(IServiceProvider provider)
        {
            bankInfoOffBLL = provider.GetRequiredService<BankInfoOffBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositService = provider.GetRequiredService<DepositService>();
            withdrawalService = provider.GetRequiredService<WithdrawalService>();
            bankInfoBLL = provider.GetRequiredService<BankInfoBLL>();
            statementService = provider.GetRequiredService<StatementService>();
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _awsDepositUrl = configBll.GetConfigValue("AWSPaymentBucketUrl");
            _backendUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").BackendService;
            singleWalletType = new Dictionary<string, string>
            {
                {"1", "lbl_Stake"  },
                {"2", "lbl_Refund" },
                {"3", "lbl_Settled"},
                {"4", "lbl_Prize"  },
                {"5", "lbl_UnSettle"}
            };
            _partitionService = provider.GetRequiredService<PartitionService>();
            _shieldService = provider.GetRequiredService<ShieldService>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();

            _tpList = statementService.GetTpListWithCache().ToDictionary(i => i.Id, i => i.TpName);
            _gameTypeList = statementService.GetGameTypeListWithCache().ToDictionary(i => i.Type, i => i.TypeName);
            _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            _logger = provider.GetRequiredService<ILogger<PaymentBLL>>();
            _iovationService = provider.GetRequiredService<IovationService>();
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _tools = provider.GetRequiredService<Tools>();
        }

        public static string GenerateVietQr(OfflineBankAccountInfo card, string transId, decimal amount)
        {
            var strAmount = amount.ToString("#");
            var strDepositId = Regex.Match(transId, @"\d+").Value;

            var bnbId = "0006" + card.BIN;
            var consumerId = $"01{card.AccountNo.Length:00}{card.AccountNo}";
            var pamentNetwork = $"01{10 + consumerId.Length}{bnbId}{consumerId}";
            var accountInfo = $"38{pamentNetwork.Length + 14 + 12}0010A000000727{pamentNetwork}0208QRIBFTTA";
            var additionalData = $"08{strDepositId.Length:00}{strDepositId}";

            var tmp = $"000201010212{accountInfo}530370454{strAmount.Length:00}{strAmount}5802VN62{additionalData.Length:00}{additionalData}6304";

            var crc = CRCCalculationExtension.CalculateCRC0xFFFF(tmp);
            var result = tmp + crc;

            return result;
        }

        public (int errorCode, string message) DepositReqLocalBank(DepositReqLocalPayModel req,
            out string transId, out string code, out OfflineBankAccountInfo bankAccountInfo)
        {
            transId = string.Empty;
            code = string.Empty;
            bankAccountInfo = null;

            if (req.Amount <= 0)
                return (-3, "Amount must be greater than 0");

            var custInfo = _custInfoBll.GetCustInfoWithCache(req.CustId);
            var bankCardInfo = bankInfoOffBLL.GetOfflineBankAccount(custInfo.SiteId, custInfo.CurrencyId)
                .FirstOrDefault(x => x.BankId == req.BankId);

            if (bankCardInfo == null)
            {

                return (-4, $"Cannot found bank card info ({req.BankId}).");
            }

            bankAccountInfo = bankCardInfo;
            var depositDetailInfo = new DepositDetailInfo
            {
                Amount = req.Amount,
                CustId = req.CustId,
                DepositType = (int)DepositType.LocalBank,
                Memo = $"LocalPayment:{(int)req.Type}:{req.Type}",
                SiteId = custInfo.SiteId,
                UserName = custInfo.UserName,
                CurrencyId = custInfo.CurrencyId
            };

            if (req.Type == DepositType.QrPayPayment)
            {
                code = CommonExtension.GenerateRandomNumber();
            }

            var depositInfo3_1 = new DepositInfo3_1
            {
                AccountName = bankCardInfo.AccountName,
                CurrencyId = custInfo.CurrencyId,
                SysId = bankCardInfo.SysId,
                AccountNo = bankCardInfo.AccountNo,
                Ver = 1,
                IsLocalPay = true,
                GroupId = req.GroupId,
                Code = code
            };

            depositDetailInfo.DepositInfo = JsonConvert.SerializeObject(depositInfo3_1);

            _depositService.DepositReq(depositDetailInfo, out transId, out _);

            return (0, string.Empty);
        }

        public int DepositReqLocalBank(int custId, int sysId, int bankId, decimal amount, string memo, string code, out DepositRep3 depositRep3)
        {
            depositRep3 = new DepositRep3();

            if (amount <= 0)
            {
                _logger.LogInformation("Incorrect request amount: " + amount);
                return -1;
            }

            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var localBankList = bankInfoOffBLL.GetOfflineBankAccount(custInfo.SiteId, custInfo.CurrencyId)
                .Where(x =>
                    (custInfo.UserLevel > pointLevel || pointLevel == 8 || x.PointLevels.Contains(pointLevel))
                    && x.VipLevels.Contains(custInfo.UserLevel)
                    && x.SysId == sysId).ToList();

            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var depositLimitInfo = depositLimitInfoList?.Where(x =>
                x.SiteId == custInfo.SiteId
                && x.VipLevel == pointLevel
                && x.DepositType == DepositType.LocalBank
                && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                _logger.LogInformation("Local bank channel disabled.");
                return -4;
            }

            if (depositLimitInfo != null && localBankList.Any())
            {
                foreach (var localBack in localBankList)
                {
                    FixDepositLimit(localBack, depositLimitInfo);
                }
            }

            var info = localBankList.FirstOrDefault(x => bankId != 0 && x.BankId == bankId) ??
                       localBankList
                           .Where(x => (x.MinAmount == 0 && x.MaxAmount == 0) || (amount >= x.MinAmount && amount <= x.MaxAmount))
                           .MinBy(x => x.BSort);

            if (info == null)
            {
                _logger.LogInformation($"No available bank card, bankId: {bankId}, amount: {amount}");
                return -3;
            }

            var depositDetailInfo = new DepositDetailInfo
            {
                Amount = amount,
                CustId = custId,
                DepositType = (int)DepositType.LocalBank,
                Memo = memo,
                SiteId = custInfo.SiteId,
                UserName = custInfo.UserName,
                CurrencyId = custInfo.CurrencyId,
                DepositInfo = JsonConvert.SerializeObject(new DepositInfo3_1
                {
                    AccountName = info.AccountName,
                    CurrencyId = custInfo.CurrencyId,
                    SysId = info.SysId,
                    AccountNo = info.AccountNo,
                    IFSCCode = info.AccountCode,
                    Ver = 1,
                    Code = code
                })
            };

            _depositService.DepositReq(depositDetailInfo, out var transId, out var postScript);

            depositRep3.TransId = transId;
            depositRep3.PostScript = postScript;
            depositRep3.AccountNo = info.AccountNo;
            depositRep3.AccountName = info.AccountName;
            depositRep3.IFSCCode = info.AccountCode;
            depositRep3.BankName = info.BankName;
            depositRep3.BankLogo = info.BankLogo;
            depositRep3.BankSite = info.BankSite;
            depositRep3.Code = code;

            return 0;
        }

        public int DepositReqOnlineBanking(int custId, int sysId, int channelId, decimal amount,
            DepositType depositType, IDictionary<string, string> extraData, out string transId)
        {
            transId = null;

            try
            {
                if (amount <= 0)
                    throw new CustomException($"Amount: {amount} <= 0", -1);

                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);


                var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
                var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
                var onlineBankList = bankInfoBLL.GetOnlineBankList(custInfo.SiteId, depositType);
                
                var onlineBank = onlineBankList
                    .FirstOrDefault(x => pointLevel >= x.PointLevel
                                         && x.ChannelId == channelId
                                         && (x.SysId == sysId || !x.SelBank)
                                         && x.CurrencyId == custInfo.CurrencyId);

                if (onlineBank == null)
                    throw new CustomException("BankInfo is null.", -4);

                _logger.LogInformation($"{depositType}: {JsonConvert.SerializeObject(onlineBank)}");

                var depositLimitInfo = depositLimitInfoList?
                    .FirstOrDefault(x => x.SiteId == custInfo.SiteId
                                         && x.VipLevel == pointLevel
                                         && x.DepositType == depositType
                                         && x.CurrencyId == custInfo.CurrencyId);

                if (depositLimitInfo?.DepositStatus == 0)
                    throw new CustomException($"DepositType={depositType} disabled.", -3);


                if (depositLimitInfo != null && onlineBank != null)
                {
                    FixDepositLimit(onlineBank, depositLimitInfo);
                }

                if (onlineBank.MaxAmount < amount || onlineBank.MinAmount > amount)
                    throw new CustomException(
                        $"Request amount does not meet the limits. MaxAmount={onlineBank.MaxAmount}, MinAmount={onlineBank.MinAmount}", -2);

                if (sysId == 0)
                {
                    onlineBank.SelBank = false;
                    onlineBank.SysId = 0;
                    onlineBank.SysBankCode = "";
                    onlineBank.SysBankName = "";
                    onlineBank.BankLogo = "";
                    onlineBank.BankName = "";
                }

                var depositInfo = JsonConvert.SerializeObject(onlineBank)
                    .AddOrSetExtraData(extraData);

                var depositDetailInfo = new DepositDetailInfo
                {
                    Amount = amount,
                    CustId = custId,
                    DepositType = (int)depositType,
                    Memo = null,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId,
                    DepositInfo = depositInfo
                };

                _depositService.DepositReq(depositDetailInfo, out transId, out _);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"({ex.Code}) {ex.Message}");
                return ex.Code;
            }

            return 0;
        }
        public int WithdrawalReq(int custId, int sysId, string accountName, string accountNo, string iFSCCode, decimal amount, string memo, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalInfo = new WithdrawalInfo();
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (_custInfoBll.IsConvertRequired(custInfo.CurrencyId))
            {
                checkAmount /= 1000;
            }

            if (Math.Ceiling(checkAmount) != Math.Floor(checkAmount))
            {
                return -1;
            }

            withdrawalInfo.AccountName = accountName;
            withdrawalInfo.AccountNo = accountNo;
            withdrawalInfo.SysId = sysId;
            withdrawalInfo.IFSCCode = iFSCCode;
            var withdrawalProcessWay = GetWithdrawalProcessWay(custInfo, accountNo, amount, WithdrawalAccountTypeEnum.BankCard);
            var result = withdrawalService.WithdrawalReq(custId, custInfo.UserName, custInfo.CurrencyId, custInfo.SiteId, amount, memo, JsonConvert.SerializeObject(withdrawalInfo), withdrawalProcessWay, out afterAmount, out transId);

            SetAutoVerifyWay(transId, custInfo, amount);

            return result;
        }
        public int WithdrawalCryptoReq(int custId, string accountName, CryptoWalletDetail cryptoDetail, string accountNo, decimal amount, string memo,
            decimal tmpRate, decimal tmpCryptoAmount, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalCryptoInfo = new WithdrawalCryptoInfo();
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (_custInfoBll.IsConvertRequired(custInfo.CurrencyId))
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
            var withdrawalProcessWay = GetWithdrawalProcessWay(custInfo, accountNo, amount, WithdrawalAccountTypeEnum.Crypto);

            var result = withdrawalService.WithdrawalReq(custId, custInfo.UserName, custInfo.CurrencyId, custInfo.SiteId, amount, memo, JsonConvert.SerializeObject(withdrawalCryptoInfo), withdrawalProcessWay, out afterAmount, out transId);

            SetAutoVerifyWay(transId, custInfo, amount);

            return result;
        }

        public int WithdrawalEWalletReq(int custId, string accountName, int sysId, string accountNo, decimal amount, string memo, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalEWalletInfo = new WithdrawalEWalletInfo();
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (_custInfoBll.IsConvertRequired(custInfo.CurrencyId))
            {
                checkAmount /= 1000;
            }

            if (Math.Ceiling(checkAmount) != Math.Floor(checkAmount))
            {
                return -1;
            }

            var eWalletInfoSysList = bankInfoBLL.GetEWalletInfoSysListByCurrency(custInfo.CurrencyId).ToList();
            var eWalletInfoSys = eWalletInfoSysList.FirstOrDefault(x => x.SysId == sysId);
            withdrawalEWalletInfo.AccountName = accountName;
            withdrawalEWalletInfo.AccountNo = accountNo;
            withdrawalEWalletInfo.EWalletSysId = sysId;
            withdrawalEWalletInfo.EWalletName = eWalletInfoSys == null ? "" : eWalletInfoSys.EWalletName;
            withdrawalEWalletInfo.AccountType = "ewallet";
            var withdrawalProcessWay = GetWithdrawalProcessWay(custInfo, accountNo, amount, WithdrawalAccountTypeEnum.EWallet);

            var result = withdrawalService.WithdrawalReq(custId, custInfo.UserName, custInfo.CurrencyId, custInfo.SiteId, amount, memo, JsonConvert.SerializeObject(withdrawalEWalletInfo), withdrawalProcessWay, out afterAmount, out transId);

            SetAutoVerifyWay(transId, custInfo, amount);

            return result;
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoRecent(int custId)
        {
            var CustInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var depositInfoRecent = _depositService.GetDepositInfoRecent(custId, CustInfo.SiteId, 5);
            foreach (var depositInfo in depositInfoRecent)
            {
                if (depositInfo.DepositType == (int)DepositType.LocalBank)
                {
                    depositInfo.DepositRep = JsonConvert.DeserializeObject<DepositInfo3_1>(depositInfo.DepositInfo);
                    var depositInfoRef = (DepositInfo3_1)depositInfo.DepositRep;
                }

                if (!string.IsNullOrEmpty(depositInfo.Memo) && depositInfo.Memo.StartsWith("LocalPayment"))
                {
                    depositInfo.DepositType = int.Parse(depositInfo.Memo.Split(":")[1]);
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

                depositInfo.DepositReceipt = GetDepositReceipt(depositInfo.TransId, custId);
                foreach (var receipt in depositInfo.DepositReceipt)
                {
                    receipt.DepositPic = GetDepositReceiptUrl(depositInfo.TransId, receipt.ReceiptNo);
                }
            }

            return depositInfoRecent;
        }

        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoRecent(int custId)
        {
            var CustInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var withdrawalInfo = withdrawalService.GetWithdrawalInfoRecent(custId, CustInfo.SiteId, 5).ToList();
            withdrawalInfo.ForEach(x =>
            {
                List<WithdrawalMessageDetail> withdrawalMessageDetails = new List<WithdrawalMessageDetail>();
                if (!string.IsNullOrWhiteSpace(x.WithdrawalMessage))
                {
                    var withdrawalMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WithdrawalMessage>>(x.WithdrawalMessage);
                    withdrawalMessage.ForEach(y =>
                    {
                        var offset = DateTime.Now.ToDateTimeOffset(x.CustTimeZone).Offset;
                        withdrawalMessageDetails.Add(new WithdrawalMessageDetail()
                        {
                            CreateTime = y.CreateTime.ToOffset(offset),
                            ErrorTypeName = selLangBLL.GetMsg($"msg_{y.Status.ToString()}"),
                            Message = selLangBLL.GetMsg($"msg_{y.Status.ToString()}_Detail"),
                        });
                    });
                }
                x.WithdrawalMessageDetail = withdrawalMessageDetails;
            });
            return withdrawalInfo;
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoHistory(int custId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var CustInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var depositInfoHistory = _depositService.GetDepositInfoHistory(CustInfo.CustId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);

            foreach (var depositInfo in depositInfoHistory)
            {
                if (depositInfo.DepositType == (int)DepositType.LocalBank)
                {
                    depositInfo.DepositRep = JsonConvert.DeserializeObject<DepositInfo3_1>(depositInfo.DepositInfo);
                    var depositInfoRef = (DepositInfo3_1)depositInfo.DepositRep;
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

                depositInfo.DepositReceipt = GetDepositReceipt(depositInfo.TransId, custId);
                foreach (var receipt in depositInfo.DepositReceipt)
                {
                    receipt.DepositPic = GetDepositReceiptUrl(depositInfo.TransId, receipt.ReceiptNo);
                }
            }

            return depositInfoHistory;
        }
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoHistory(int custId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var CustInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var withdrawalInfoHistory = withdrawalService.GetWithdrawalInfoHistory(CustInfo.CustId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount).ToList();
            withdrawalInfoHistory.ForEach(x =>
            {
                List<WithdrawalMessageDetail> withdrawalMessageDetails = new List<WithdrawalMessageDetail>();
                if (!string.IsNullOrWhiteSpace(x.WithdrawalMessage))
                {
                    var withdrawalMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WithdrawalMessage>>(x.WithdrawalMessage);
                    withdrawalMessage.ForEach(y =>
                    {
                        var offset = DateTime.Now.ToDateTimeOffset(x.CustTimeZone).Offset;
                        withdrawalMessageDetails.Add(new WithdrawalMessageDetail()
                        {
                            CreateTime = y.CreateTime.ToOffset(offset),
                            ErrorTypeName = selLangBLL.GetMsg($"msg_{y.Status.ToString()}"),
                            Message = selLangBLL.GetMsg($"msg_{y.Status.ToString()}_Detail"),
                        });
                    });
                }
                x.WithdrawalMessageDetail = withdrawalMessageDetails;
            });
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
                if (count > 0)
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
                if (string.IsNullOrWhiteSpace(TypeName))
                {
                    int.TryParse(Type, out int intType);
                    if (intType == (int)FinancialType.Deposit || intType.IsBetween(101, 1000))
                    {
                        return "Deposit";
                    }
                }
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
        public async Task<int> SetDepositReceiptPic(int custId, string transId, int receiptNo, string picFileName, string userName)
        {
            var depositId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            return await _depositService.SetDepositReceiptPic(custId, depositId, receiptNo, picFileName, userName);
        }
        public int SetDepositCancel(int custId, string transId)
        {
            var depositId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            var CustInfo = _custInfoBll.GetCustInfoWithCache(custId);
            return _depositService.SetDepositCancel(CustInfo.CustId, depositId, CustInfo.SiteId);
        }

        public int SetWithdrawalCancel(int custId, string transId)
        {
            var CustInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var forStatus = new List<int>
            {
                (int)WithdrawalStatus.Pending,
                (int)WithdrawalStatus.ToKyc
            };
            return withdrawalService.SetWithdrawalCancel(CustInfo.CustId, transId, CustInfo.SiteId, forStatus, WithdrawalStatus.Cancelled);
        }
        public IEnumerable<DepositReceipt> GetDepositReceipt(string transId, int custId)
        {
            var depositId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            var CustInfo = _custInfoBll.GetCustInfoWithCache(custId);
            return _depositService.GetDepositReceipt(depositId, CustInfo.SiteId);
        }

        public string GetDepositReceiptUrl(string transId, int receiptNo)
        {
            using var client = new HttpClient();
            var url = $"{_awsDepositUrl}deposit/{transId}_{receiptNo}.png";
            var picUrl = new RedirectResult(url).Url;
            var picRsp = client.GetAsync(picUrl).Result;
            if (picRsp.StatusCode == HttpStatusCode.OK)
            {
                return picUrl;
            }

            return null;
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
            return statementService.GetCustTpBetRecordDateRange(custId, startDate, endDate, PageSize, ref PageNumber, ref TotalPages, ref RowCount);
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
                .GroupBy(x => new { x.CurrencyId, x.CurrencyName })
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

        public CryptoInfoData GetSupportCryptoInfoWithCache(int siteId)
        {
            return _cacheHelper.TryGetOrCreate($"PaySetting:SupportCrypto:{siteId}", TimeSpan.FromMinutes(5),
                () => GetSupportCryptoInfo(siteId));
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

        public WithdrawalInfoAutoWayEnum GetWithdrawalProcessWay(Customer custInfo, string accountNo, decimal withdrawalAmount, WithdrawalAccountTypeEnum accountType)
        {
            var setting = withdrawalService.GetAutoPaymentRuleSetting(custInfo.SiteId, custInfo.CurrencyId, accountType, AutoType.AutoApproved);
            if (setting is null)
                return WithdrawalInfoAutoWayEnum.Manual; //沒有該幣別的設定就是手動

            var withdrawalSuccessCount = withdrawalService.GetWithdrawalSuccessCount(custInfo.CustId, accountNo);

            var check = CheckAutoPaymentRuleSetting(setting, custInfo.TagList, custInfo.UserLevel, withdrawalAmount, withdrawalSuccessCount);

            _logger.LogInformation($"GetWithdrawalProcessWay GetAutoWithdrawalRuleSetting：{JsonConvert.SerializeObject(setting)}，withdrawalSuccessCounter：{withdrawalSuccessCount},CustId:{custInfo.CustId},SiteId：{custInfo.SiteId},CurrencyId:{custInfo.CurrencyId},accountNo:{accountNo}");

            return check;
        }

        private void SetAutoVerifyWay(string transId, Customer custInfo, decimal withdrawalAmount)
        {
            try
            {
                var withdrawalVerifyWay = GetWithdrawalVerifyWay(transId, custInfo, withdrawalAmount);
                if (withdrawalVerifyWay == WithdrawalInfoAutoWayEnum.Auto)
                {
                    _ = AutoVerify(transId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, $"SetAutoVerifyWay Exception Failed ,TransId:{transId}");
            }
        }

        private WithdrawalInfoAutoWayEnum GetWithdrawalVerifyWay(string transId, Customer custInfo, decimal withdrawalAmount)
        {
            var setting = withdrawalService.GetAutoPaymentRuleSetting(custInfo.SiteId, custInfo.CurrencyId, 0, AutoType.AutoVerify);
            if (setting is null)
                return WithdrawalInfoAutoWayEnum.Manual; //沒有該幣別的設定就是手動

            // 不看卡號 只看提款成功次數
            var withdrawalSuccessCount = withdrawalService.GetAllAccountWithdrawalSuccessCounterByCustId(custInfo.CustId);
            if (custInfo.SiteId == 1)
            {
                // Iovation
                var custIovation = _iovationService.GetCustIovation(custInfo.CustId);
                if (custIovation is null || !setting.DeviceLevel.Contains(custIovation.Iovation))
                {
                    _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, custIovation : {custIovation} , Setting :{JsonConvert.SerializeObject(setting.DeviceLevel)} ");
                    return WithdrawalInfoAutoWayEnum.Manual;
                }
                _logger.LogInformation("setp 34.");
            }
            else
            {
                // shieldScores
                var context = _httpContextAccessor.HttpContext;
                var strDeviceScore = context.Request.Cookies["device_score"];
                var deviceScore = -1;
                if (strDeviceScore != null)
                {
                    deviceScore = Convert.ToInt32(strDeviceScore);
                }
                else
                {
                    var shieldInfo = _shieldService.GetCustShieldRiskInfoList(custInfo.CustId);
                    if (shieldInfo is not null)
                    {
                        deviceScore = shieldInfo.DeviceScore;
                        _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, Can't find device_score , use DB Data : {shieldInfo.DeviceScore}  ");
                    }
                }

                if (deviceScore < setting.DeviceScores)
                {
                    _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, deviceScore : {deviceScore} , Setting :{setting.DeviceScores} ");
                    return WithdrawalInfoAutoWayEnum.Manual;
                }
            }
            var check = CheckAutoPaymentRuleSetting(setting, custInfo.TagList, custInfo.UserLevel, withdrawalAmount, withdrawalSuccessCount);

            return check;
        }

        private WithdrawalInfoAutoWayEnum CheckAutoPaymentRuleSetting(AutoPaymentRuleSettingDto setting, string tagList, int customerVip, decimal withdrawalAmount, int withdrawalSuccessCount)
        {
            var customerTags = string.IsNullOrEmpty(tagList)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(tagList);

            // 玩家身上有Tag，設定也有tag 才檢查
            if (customerTags.Any() && setting.Tags.Any())
            {
                if (!customerTags.Intersect(setting.Tags).Any())
                {
                    return WithdrawalInfoAutoWayEnum.Manual;
                }
            }

            // vipLevelCheck
            if (setting.VipLevels.Any())
            {
                if (setting.VipLevels.All(x => x != customerVip))
                {
                    return WithdrawalInfoAutoWayEnum.Manual;
                }
            }

            // withdrawal Amount check
            if (withdrawalAmount > setting.WithdrawalAmount)
            {
                return WithdrawalInfoAutoWayEnum.Manual;
            }

            // withdrawal SuccessCounter check
            if (withdrawalSuccessCount < setting.AccountWithdrawalCount)
            {
                return WithdrawalInfoAutoWayEnum.Manual;
            }

            return WithdrawalInfoAutoWayEnum.Auto;
        }

        private ApiResult AutoVerify(string transId)
        {
            var apiUrl = $"{_backendUrl}ApiPaymentGateWay/AutoPayment";
            var req = new AutoPaymentRequest
            {
                TransId = transId,
                Token = _tools.CreateBackendToken(transId),
                Comment = "Auto Payment Verify"
            };

            var result = _courierAdapter.MyPostAsync<ApiResult, AutoPaymentRequest>(apiUrl, req).Result;

            return result;
        }
    }
}
