using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class PaymentBLL
    {
        private readonly BankInfoOffBLL bankInfoOffBLL;
        private readonly CustInfoBll custInfoBLL;
        private readonly DepositService _depositService;
        private readonly WithdrawalService withdrawalService;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly RolloverService rollOverService;
        private readonly StatementService statementService;
        private readonly SelLangBLL selLangBLL;
        private readonly Dictionary<string, string> singleWalletType;

        public PaymentBLL(BankInfoBLL bankInfoBLL, BankInfoOffBLL bankInfoOffBLL, CustInfoBll custInfoBLL, DepositService depositService, WithdrawalService withdrawalService, RolloverService rollOverService, StatementService statementService, SelLangBLL selLangBLL)
        {
            this.bankInfoOffBLL = bankInfoOffBLL;
            this.custInfoBLL = custInfoBLL;
            this._depositService = depositService;
            this.withdrawalService = withdrawalService;
            this.bankInfoBLL = bankInfoBLL;
            this.rollOverService = rollOverService;
            this.statementService = statementService;
            this.selLangBLL = selLangBLL;
          
            singleWalletType = new Dictionary<string, string>
            {
                {"1", "lbl_Stake"  },
                {"2", "lbl_Refund" },
                {"3", "lbl_Settled"},
                {"4", "lbl_Prize"  },
                {"5", "lbl_UnSettle"}
            };
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
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var bankAccountOffList = bankInfoOffBLL.GetBankAccountOff(custInfo.SiteId);
            var localBankList = bankAccountOffList?.ToList();
            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var depositLimitInfo = depositLimitInfoList?.FirstOrDefault(x =>
                x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.LocalBank &&
                x.CurrencyId == custInfo.CurrencyId);

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

            var _info = (from x in localBankList
                         where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId && ((x.MinAmount == 0 && x.MaxAmount == 0) || (amount >= x.MinAmount && amount <= x.MaxAmount))
                         orderby x.PointLevel descending, x.BSort
                         select new
                         {
                             x.SysId,
                             x.BankId,
                             x.BankLogo,
                             x.BankName,
                             x.BankSite,
                             x.AccountName,
                             x.AccountNo,
                             x.IFSCCode
                         }).FirstOrDefault();

            if (_info != null)
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
                    AccountName = _info.AccountName,
                    CurrencyId = custInfo.CurrencyId,
                    SysId = _info.SysId,
                    AccountNo = _info.AccountNo,
                    IFSCCode = _info.IFSCCode,
                    Ver = 1
                };
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositInfo3_1);

                _depositService.DepositReq(depositDetailInfo, out var transId, out var postScript);
                depositRep3.TransId = transId;
                depositRep3.PostScript = postScript;
                depositRep3.AccountNo = depositInfo3_1.AccountNo;
                depositRep3.AccountName = depositInfo3_1.AccountName;
                depositRep3.IFSCCode = depositInfo3_1.IFSCCode;
                depositRep3.BankName = _info.BankName;
                depositRep3.BankLogo = _info.BankLogo;
                depositRep3.BankSite = _info.BankSite;

                return 0;
            }

            var _info1 = (from x in localBankList
                          where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId
                          select new
                          {
                              x.SysId,
                              x.BankId,
                              x.BankLogo,
                              x.BankName,
                              x.BankSite,
                              x.BSort,
                              x.AccountName,
                              x.AccountNo,
                              x.IFSCCode,
                              x.MinAmount,
                              x.MaxAmount
                          }).OrderByDescending(x => x.MaxAmount).ThenBy(x => x.MinAmount).ThenBy(x => x.BSort).FirstOrDefault();
            if (_info == null && _info1 == null)
            {
                return -2;
            }

            if (_info == null && _info1 != null)
            {
                depositRep3.MaxAmount = _info1.MaxAmount;
                depositRep3.MinAmount = _info1.MinAmount;

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
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var bankAccountOffList = bankInfoOffBLL.GetBankAccountOff(custInfo.SiteId);
            var localBankList = bankAccountOffList?.ToList();
            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.LocalBank && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

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

            var _info = (from x in localBankList
                         where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId && ((x.MinAmount == 0 && x.MaxAmount == 0) || (amount >= x.MinAmount && amount <= x.MaxAmount))
                         orderby x.PointLevel descending, x.BSort
                         select new
                         {
                             x.SysId,
                             x.BankId,
                             x.BankLogo,
                             x.BankName,
                             x.BankSite,
                             x.AccountName,
                             x.AccountNo,
                             x.IFSCCode
                         }).FirstOrDefault();

            if (_info != null)
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
                    AccountName = _info.AccountName,
                    CurrencyId = custInfo.CurrencyId,
                    SysId = _info.SysId,
                    AccountNo = _info.AccountNo,
                    IFSCCode = _info.IFSCCode,
                    Ver = 1
                };
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositInfo3_1);
                
                _depositService.DepositReqwithFrom(depositDetailInfo, depositFromInfo, out var transId, out var postScript);

                depositRep3.TransId = transId;
                depositRep3.PostScript = postScript;
                depositRep3.AccountNo = depositInfo3_1.AccountNo;
                depositRep3.AccountName = depositInfo3_1.AccountName;
                depositRep3.IFSCCode = depositInfo3_1.IFSCCode;
                depositRep3.BankName = _info.BankName;
                depositRep3.BankLogo = _info.BankLogo;
                depositRep3.BankSite = _info.BankSite;

                return 0;
            }

            var _info1 = (from x in localBankList
                          where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId
                          select new
                          {
                              x.SysId,
                              x.BankId,
                              x.BankLogo,
                              x.BankName,
                              x.BankSite,
                              x.BSort,
                              x.AccountName,
                              x.AccountNo,
                              x.IFSCCode,
                              x.MinAmount,
                              x.MaxAmount
                          }).OrderByDescending(x => x.MaxAmount).ThenBy(x => x.MinAmount).ThenBy(x => x.BSort).FirstOrDefault();
            if (_info == null && _info1 == null)
            {
                return -2;
            }

            if (_info == null && _info1 != null)
            {
                depositRep3.MaxAmount = _info1.MaxAmount;
                depositRep3.MinAmount = _info1.MinAmount;

                return -3;
            }

            return 0;
        }

        public int LocalBankinfo(int custId, int sysId, string memo, out DepositRep3 depositRep3)
        {
            depositRep3 = new DepositRep3();

            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var bankAccountOffList = bankInfoOffBLL.GetBankAccountOff(custInfo.SiteId);
            var localBankList = bankAccountOffList?.ToList();
            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.LocalBank && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

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

            var _info = (from x in localBankList
                         where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId 
                         orderby x.PointLevel descending, x.BSort
                         select new
                         {
                             x.SysId,
                             x.BankId,
                             x.BankLogo,
                             x.BankName,
                             x.BankSite,
                             x.AccountName,
                             x.AccountNo,
                             x.IFSCCode
                         }).FirstOrDefault();

            if (_info != null)
            {
                var depositDetailInfo = new DepositDetailInfo
                {
                    CustId = custId,
                    DepositType = (int)DepositType.LocalBank,
                    Memo = memo,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId
                };

                var depositInfo3_1 = new DepositInfo3_1
                {
                    AccountName = _info.AccountName,
                    CurrencyId = custInfo.CurrencyId,
                    SysId = _info.SysId,
                    AccountNo = _info.AccountNo,
                    IFSCCode = _info.IFSCCode,
                    Ver = 1
                };
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositInfo3_1);

                //depositService.DepositReq(depositDetailInfo, out var transId, out var postScript);
                var transId = "";
                var postScript = "";
                depositRep3.TransId = transId;
                depositRep3.PostScript = postScript;
                depositRep3.AccountNo = depositInfo3_1.AccountNo;
                depositRep3.AccountName = depositInfo3_1.AccountName;
                depositRep3.IFSCCode = depositInfo3_1.IFSCCode;
                depositRep3.BankName = _info.BankName;
                depositRep3.BankLogo = _info.BankLogo;
                depositRep3.BankSite = _info.BankSite;

                return 0;
            }

            var _info1 = (from x in localBankList
                          where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId
                          select new
                          {
                              x.SysId,
                              x.BankId,
                              x.BankLogo,
                              x.BankName,
                              x.BankSite,
                              x.BSort,
                              x.AccountName,
                              x.AccountNo,
                              x.IFSCCode,
                              x.MinAmount,
                              x.MaxAmount
                          }).OrderByDescending(x => x.MaxAmount).ThenBy(x => x.MinAmount).ThenBy(x => x.BSort).FirstOrDefault();
            if (_info == null && _info1 == null)
            {
                return -2;
            }

            if (_info == null && _info1 != null)
            {
                depositRep3.MaxAmount = _info1.MaxAmount;
                depositRep3.MinAmount = _info1.MinAmount;

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
            var payCurrency = bankInfoBLL.GetPayCurrency().FirstOrDefault(x => x.ChannelId == channelId && custInfo.CurrencyId == x.CurrencyId);

            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfo = depositLimitInfoList?.FirstOrDefault(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)depositType && x.CurrencyId == custInfo.CurrencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            if (depositLimitInfo != null && payCurrency != null)
            {
                FixDepositLimit(payCurrency, depositLimitInfo);
            }

            if (payCurrency == null || payCurrency.MaxAmount < amount || payCurrency.MinAmount > amount)
            {
                return -2;
            }

            var onlineBankList = depositType == DepositType.OnlineBanking ? 
                bankInfoBLL.GetOnlineBankList(custInfo.SiteId) : 
                bankInfoBLL.GetAutoChannelBankInfoList(custInfo.SiteId, depositType);
            onlineBank = onlineBankList.FirstOrDefault(x => vipLevel >= x.PointLevel && x.ChannelId == channelId && (x.SysId == sysId || !x.SelBank) && x.PayId == payId && (x.CurrencyId == custInfo.CurrencyId || !x.SelBank));

            if (onlineBank != null)
            {
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

            return -99;
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
        public int WithdrawalUSDTReq(int custId, string accountName, USDTDetail uSDTDetail, string accountNo, decimal amount, string memo, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            var withdrawalUSDTInfo = new WithdrawalUSDTInfo();
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

            var currencyEnum = (CryptoCurrencyId) uSDTDetail.CurrencyId;

            withdrawalUSDTInfo.AccountType = "crypto";
            withdrawalUSDTInfo.CryptoCurrency = currencyEnum.ToString();
            withdrawalUSDTInfo.AccountName = accountName;
            withdrawalUSDTInfo.NickName = uSDTDetail.NickName;
            withdrawalUSDTInfo.AccountNo = accountNo;
            withdrawalUSDTInfo.Network = uSDTDetail.Network;
            withdrawalUSDTInfo.CurrencyId = uSDTDetail.CurrencyId;
            
            return withdrawalService.WithdrawalUSDTReq(custId, CustInfo.UserName, CustInfo.CurrencyId, CustInfo.SiteId, amount, memo, withdrawalUSDTInfo, out afterAmount, out transId);
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
            }
            return depositInfoHistory;
        }
        public IEnumerable<WithdrawalHistory> GetWithdrawalInfoHistory(int custId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var withdrawalInfoHistory = withdrawalService.GetWithdrawalInfoHistory(CustInfo.CustId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);
            return withdrawalInfoHistory;
        }

        public IEnumerable<Statement> GetStatement(int custId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var datas = statementService.GetStatement(CustInfo.CustId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);

            //var tpList = statementService.GetTpList();

            //Dictionary<string, string> singleWalletTransType = new Dictionary<string, string>()
            //{
            //    {"1", selLangBLL.GetResourceValue("lbl_Stake")  }, 
            //    {"2", selLangBLL.GetResourceValue("lbl_Refund") }, 
            //    {"3", selLangBLL.GetResourceValue("lbl_Settled")},
            //    {"4", selLangBLL.GetResourceValue("lbl_Prize")  }, 
            //    {"5", selLangBLL.GetResourceValue("lbl_UnSettle")}
            //};

            return datas.Select(x => new Statement
            {
                BeforeAmount = x.BeforeAmount,
                ChangeAmount = x.ChangeAmount,
                CreateTime = x.CreateTime,
                Direction = x.Direction,
                RefTransId = x.RefTransId,
                AfterAmount = x.AfterAmount,
                //TypeName = GetTransTypeName(x.Type, x.TypeName, ref tpList, ref singleWalletTransType)
                TypeName = GetTransTypeNameV2(x.Type, x.TypeName)

            });
        }

        public IEnumerable<BounsLogStatement> GetBounsLogStatement(int custId,int PromoCategoryId, long startDate, long endDate, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var datas = statementService.GetBounsLogStatement(CustInfo.CustId, PromoCategoryId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);
            //var tpList = statementService.GetTpList();

            //Dictionary<string, string> singleWalletTransType = new Dictionary<string, string>()
            //{
            //    {"1", selLangBLL.GetResourceValue("lbl_Stake")  },
            //    {"2", selLangBLL.GetResourceValue("lbl_Refund") },
            //    {"3", selLangBLL.GetResourceValue("lbl_Settled")},
            //    {"4", selLangBLL.GetResourceValue("lbl_Prize")  },
            //    {"5", selLangBLL.GetResourceValue("lbl_UnSettle")}
            //};

            return datas.Select(x => new BounsLogStatement
            {
                BeforeBonus = x.BeforeBonus,
                ChangeBonus = x.ChangeBonus,
                CreateTime = x.CreateTime,
                Direction = x.Direction,
                RefTransId = x.RefTransId,
                AfterBonus = x.AfterBonus,
                //TypeName = GetTransTypeName(x.Type, x.TypeName, ref tpList, ref singleWalletTransType)
                TypeName = GetTransTypeNameV2(x.Type, x.TypeName)
            });
        }
        private string GetTransTypeNameV2(string Type, string TypeName)
        {
            if (Type.Length < 5)
            {
                return TypeName;
            }
            else
            {
                var tpList = statementService.GetTpListWithCache();
                var gameTypeList = statementService.GetGameTypeListWithCache();

                singleWalletType.TryGetValue(Type.Substring(1, 1), out var WalletType);
                int.TryParse(Type.Substring(2), out var tempId);
                string tpName = null;

                if (Type.StartsWith("1"))
                {
                    tpName = tpList.FirstOrDefault(x => x.Id == tempId)?.TpName;
                }
                else if (Type.StartsWith("2"))
                {
                    tpName = gameTypeList.FirstOrDefault(x => x.Type == tempId)?.TypeName;
                }

                return $"{tpName} {selLangBLL.GetResourceValue(WalletType)}";
            }
        }

        public IEnumerable<PromotionCategory> GetPromotionCategory()
        {
            return statementService.GetPromotionCategory();
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
            var forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Pending);
            forStatus.Add((int)WithdrawalStatus.ToKyc);
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
        public Rollover ReachedRollover(int custId)
        {
            return rollOverService.ReachedRollover(custId);
        }
        public decimal GetWithdrawalAvailableBalance(int custId)
        {
            return withdrawalService.GetWithdrawalAvailableBalance(custId);
        }
        public WithdrawalLimitInfo GetCustWithdrawalLimitInfo(int custId)
        {
            return withdrawalService.GetCustWithdrawalLimitInfo(custId);
        }

        private void FixDepositLimit(IPaymentLimit paymentLimit, DepositLimitInfo depositLimitInfo)
        {
            if (depositLimitInfo.DepositMin > paymentLimit.MaxAmount || depositLimitInfo.DepositMax < paymentLimit.MinAmount)
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
        public IEnumerable<TpBetRecordListOneWeek> GetCustTpBetRecordListOneWeek(int custId)
        {
            return statementService.GetCustTpBetRecordListOneWeek(custId);
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
        public IEnumerable<NetworkList> GetSupportNetwork( int currencyId)
        {
            return statementService.GetSupportNetwork(currencyId);
        }
        public CryptoInfoData GetSupportCryptoInfo()
        {
            var infos = statementService.GetSupportCryptoInfo();
            var cryptoCurrency = infos
                .Select(x => new CryptoCurrency
                {
                    CurrencyId = x.CurrencyId,
                    CurrencyName = x.CurrencyName
                }).Distinct(new CryptoCurrencyComparer());

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
        public WithdrawalLimitInfoOutput GetUSDTWithdrawalLimitInfo(USDTWithdrawalLimitInfoInput input)
        {
            return withdrawalService.GetUSDTWithdrawalLimitInfo(input);
        }

        public WithdrawalLimitInfoOutput GetUSDTWithdrawalLimitInfoV2(USDTWithdrawalLimitInfoInput input)
        {
            return withdrawalService.GetUSDTWithdrawalLimitInfoV2(input);
        }

        public IEnumerable<USDTWithdrawalLimitInfoOutput> GetUSDTWithdrawalLimitInfoList(USDTWithdrawalLimitInfoInput input)
        {
            return withdrawalService.GetUSDTWithdrawalLimitInfoList(input);
        }
    }
}
