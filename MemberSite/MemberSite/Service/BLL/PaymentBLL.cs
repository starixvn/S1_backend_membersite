using System.Net;
using MemberSite.Extensions;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Interfaces;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

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
            custInfoBLL = provider.GetRequiredService<CustInfoBll>();
            _depositService = provider.GetRequiredService<DepositService>();
            withdrawalService = provider.GetRequiredService<WithdrawalService>();
            bankInfoBLL = provider.GetRequiredService<BankInfoBLL>();
            statementService = provider.GetRequiredService<StatementService>();
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            _awsDepositUrl = configuration.GetValue<string>("AwsDepositUrl");
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

            _tpList = statementService.GetTpListWithCache().ToDictionary(i => i.Id, i => i.TpName);
            _gameTypeList = statementService.GetGameTypeListWithCache().ToDictionary(i => i.Type, i => i.TypeName);
            _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            _logger = provider.GetRequiredService<ILogger<PaymentBLL>>();
            _iovationService = provider.GetRequiredService<IovationService>();
            _backendUrl = configuration.GetValue<string>("BackendService");
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
            
            var tmp =$"000201010212{accountInfo}530370454{strAmount.Length:00}{strAmount}5802VN62{additionalData.Length:00}{additionalData}6304";
            
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
            
            var custInfo = custInfoBLL.GetCustInfoWithCache(req.CustId);
            var bankCardInfo = bankInfoOffBLL.GetOfflineBankAccount(custInfo.SiteId)
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
                DepositType = (int) DepositType.LocalBank,
                Memo = $"LocalPayment:{(int) req.Type}:{req.Type}",
                SiteId = custInfo.SiteId,
                UserName = custInfo.UserName,
                CurrencyId = custInfo.CurrencyId
            };

            if (req.Type == DepositType.QRPayPayment)
            {
                var random = new Random();
                code = random.Next(0, 1000000).ToString("000000");
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

            _depositService.DepositReq(depositDetailInfo, out transId, out var postScript);
                
            return (0, string.Empty);
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
        public int DepositReqLocalBank(int custId, int sysId, int bankId, decimal amount, string memo, out DepositRep3 depositRep3)
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

            var info = localBankList.FirstOrDefault(x => bankId != 0 && x.BankId == bankId) ??
                       localBankList
                .Where(x => (x.MinAmount == 0 && x.MaxAmount == 0) || (amount >= x.MinAmount && amount <= x.MaxAmount))
                .OrderBy(x => x.BSort).FirstOrDefault();

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
                    IFSCCode = info.AccountCode,
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

        public int DepositReqLocalBankWithFrom(int custId, int sysId, int bankId, decimal amount, string memo, string depositFromInfo, out DepositRep3 depositRep3)
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

            var info = localBankList.FirstOrDefault(x => bankId != 0 && x.BankId == bankId) ??
                       localBankList
                           .Where(x => (x.MinAmount == 0 && x.MaxAmount == 0) || (amount >= x.MinAmount && amount <= x.MaxAmount))
                           .OrderBy(x => x.BSort).FirstOrDefault();

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
                    IFSCCode = info.AccountCode,
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

            onlineBank = onlineBankList
                .FirstOrDefault(x => pointLevel >= x.PointLevel
                                     && x.ChannelId == channelId
                                     && (x.SysId == sysId || !x.SelBank)
                                     && x.PayId == payId
                                     && x.CurrencyId == custInfo.CurrencyId);

            _logger.LogInformation("onlineBank: " + JsonConvert.SerializeObject(onlineBank));

            var depositLimitInfo = depositLimitInfoList?
                .FirstOrDefault(x => x.SiteId == custInfo.SiteId
                                     && x.VipLevel == pointLevel
                                     && x.DepositType == (int)depositType
                                     && x.CurrencyId == custInfo.CurrencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            if (depositLimitInfo != null && onlineBank != null)
            {
                FixDepositLimit(onlineBank, depositLimitInfo);
            }

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

            _depositService.DepositReq(depositDetailInfo, out transId, out var postScript);

            return 0;
        }
        public int WithdrawalReq(int custId, int sysId, string accountName, string accountNo, string iFSCCode, decimal amount, string memo, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalInfo = new WithdrawalInfo();
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (custInfoBLL.IsConvertRequired(custInfo.CurrencyId))
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

            SetAutoVerifyWay(transId, custInfo, accountNo, amount);
            
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
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (custInfoBLL.IsConvertRequired(custInfo.CurrencyId))
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
            
            SetAutoVerifyWay(transId, custInfo, accountNo, amount);

            return result;
        }

        public int WithdrawalEWalletReq(int custId, string accountName, int sysId, string accountNo, decimal amount, string memo, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalEWalletInfo = new WithdrawalEWalletInfo();
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var checkAmount = amount;
            if (custInfoBLL.IsConvertRequired(custInfo.CurrencyId))
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
            
            SetAutoVerifyWay(transId, custInfo, accountNo, amount);

            return result;
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoRecent(int custId)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var depositInfoRecent = _depositService.GetDepositInfoRecent(custId, CustInfo.SiteId, 5);
            foreach (var depositInfo in depositInfoRecent)
            {
                if (depositInfo.DepositType == (int)DepositType.LocalBank)
                {
                    depositInfo.DepositRep = JsonConvert.DeserializeObject<DepositInfo3_1>(depositInfo.DepositInfo);
                    var depositInfoRef = (DepositInfo3_1)depositInfo.DepositRep;
                    depositInfo.BankInfo = bankInfoBLL.GetBankInfoSysListByCurrency(CustInfo.CurrencyId).FirstOrDefault(x => x.SysId == depositInfoRef.SysId);
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
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
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
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
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
        public IEnumerable<DepositReceipt> GetDepositReceipt(string transId, int custId)
        {
            var depositId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
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

        public WithdrawalInfoProcessWayEnum GetWithdrawalProcessWay(Customer custInfo, string accountNo, decimal withdrawalAmount, WithdrawalAccountTypeEnum accountType)
        {
            var setting = withdrawalService.GetAutoWithdrawalRuleSetting(custInfo.SiteId, custInfo.CurrencyId);
            if (setting is null)
                return WithdrawalInfoProcessWayEnum.Manual; //沒有該幣別的設定就是手動

            var withdrawalSuccessCounter = withdrawalService.GetWithdrawalSuccessCounter(custInfo.CustId, accountNo);
            var customerAccountType = (int)accountType;
            var customerVip = custInfo.UserLevel;
            var customerTags = string.IsNullOrEmpty(custInfo.TagList)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(custInfo.TagList);

            // 玩家身上有Tag，設定也有tag 才檢查
            if (customerTags.Any() && setting.Tags.Any())
            {
                if (!customerTags.Intersect(setting.Tags).Any())
                    return WithdrawalInfoProcessWayEnum.Manual;
            }

            // vipLevelCheck
            if (setting.VipLevels.Any())
            {
                if (setting.VipLevels.All(x => x != customerVip))
                    return WithdrawalInfoProcessWayEnum.Manual;
            }

            // withdrawal AccountType check
            if (setting.WithdrawalTypes.Any())
            {
                if (setting.WithdrawalTypes.All(r => r != customerAccountType))
                    return WithdrawalInfoProcessWayEnum.Manual;
            }

            // withdrawal Amount check
            if (withdrawalAmount > setting.WithdrawalAmount)
                return WithdrawalInfoProcessWayEnum.Manual;

            // withdrawal SuccessCounter check
            if (withdrawalSuccessCounter.SuccessWithdrawalCount < setting.AccountWithdrawalCount)
                return WithdrawalInfoProcessWayEnum.Manual;

            _logger.LogInformation($"GetWithdrawalProcessWay GetAutoWithdrawalRuleSetting：{JsonConvert.SerializeObject(setting)}，withdrawalSuccessCounter：{JsonConvert.SerializeObject(withdrawalSuccessCounter)},SiteId：{custInfo.SiteId},CurrencyId:{custInfo.CurrencyId},accountNo:{accountNo}");


            return WithdrawalInfoProcessWayEnum.Auto;
        }

        private void SetAutoVerifyWay(string transId, Customer custInfo, string accountNo, decimal withdrawalAmount)
        {
            try
            {
                var withdrawalVerifyWay = GetWithdrawalVerifyWay(transId, custInfo, accountNo, withdrawalAmount);
                if (withdrawalVerifyWay == WithdrawalInfoVerifyWayEnum.Auto)
                {
                    var _ = AutoVerify(transId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, $"SetAutoVerifyWay Exception Failed ,TransId:{transId}");
            }
        }

        private WithdrawalInfoVerifyWayEnum GetWithdrawalVerifyWay(string transId, Customer custInfo, string accountNo, decimal withdrawalAmount)
        {
            var setting = withdrawalService.GetAutoPaymentRuleSetting(custInfo.SiteId, custInfo.CurrencyId,AutoType.AutoVerify);
            if (setting is null)
                return WithdrawalInfoVerifyWayEnum.Manual; //沒有該幣別的設定就是手動

            // 不看卡號 只看提款成功次數
            var withdrawalSuccessCounter = withdrawalService.GetAllAccountWithdrawalSuccessCounterByCustId(custInfo.CustId);
 
            var customerVip = custInfo.UserLevel;
            var customerTags = string.IsNullOrEmpty(custInfo.TagList)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(custInfo.TagList);

            if (custInfo.SiteId == (int) SiteId.Nova88)
            {
                // Iovation
                var custIovation = _iovationService.GetCustIovation(custInfo.CustId);
                if (!setting.DeviceLevel.Contains(custIovation.Iovation))
                {
                    _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, custIovation : {custIovation} , Setting :{JsonConvert.SerializeObject(setting.DeviceLevel)} " );
                    return WithdrawalInfoVerifyWayEnum.Manual;;
                }
                   
            }
            else
            {
                // shieldScores
                var context = _httpContextAccessor.HttpContext;
                var strDeviceScore = context.Request.Cookies["device_score"];
                var deviceScore = strDeviceScore != null ? Convert.ToInt32(strDeviceScore) : -1;

                if (deviceScore < setting.DeviceScores)
                {
                    _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, deviceScore : {deviceScore} , Setting :{setting.DeviceScores} " );
                    return WithdrawalInfoVerifyWayEnum.Manual;
                }
            }
            
            // 玩家身上有Tag，設定也有tag 才檢查
            if (customerTags.Any() && setting.Tags.Any())
            {
                if (!customerTags.Intersect(setting.Tags).Any())
                {
                    _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, customerTags : {JsonConvert.SerializeObject(customerTags)} , Setting :{JsonConvert.SerializeObject(setting.Tags)} " );
                    return WithdrawalInfoVerifyWayEnum.Manual;
                }
            }

            // vipLevelCheck
            if (setting.VipLevels.Any())
            {
                if (setting.VipLevels.All(x => x != customerVip))
                {
                    _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, customerVip : {customerVip} , Setting :{JsonConvert.SerializeObject(setting.VipLevels)} " );
                    return WithdrawalInfoVerifyWayEnum.Manual;
                }
            }
            
            // withdrawal Amount check
            if (withdrawalAmount > setting.WithdrawalAmount)
            {
                _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, withdrawalAmount : {withdrawalAmount} , Setting :{setting.WithdrawalAmount} " );
                return WithdrawalInfoVerifyWayEnum.Manual;
            }

            // withdrawal SuccessCounter check
            if (withdrawalSuccessCounter.SuccessWithdrawalCount < setting.AccountWithdrawalCount)
            {
                _logger.LogInformation($"{transId} WithdrawalVerifyWay Manual, SuccessWithdrawalCount : {withdrawalSuccessCounter.SuccessWithdrawalCount} , Setting :{setting.AccountWithdrawalCount} " );
                return WithdrawalInfoVerifyWayEnum.Manual;
            }

            return WithdrawalInfoVerifyWayEnum.Auto;
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
            
            return result ;
        }
    }
}
