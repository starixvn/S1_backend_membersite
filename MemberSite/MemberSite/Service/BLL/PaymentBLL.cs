using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class PaymentBLL
    {
        private readonly BankInfoOffBLL bankInfoOffBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly DepoistService depoistService;
        private readonly WithdrawalService withdrawalService;
        private readonly BankInfoBLL bankInfoBLL;

        public PaymentBLL(BankInfoBLL bankInfoBLL, BankInfoOffBLL bankInfoOffBLL, CustInfoBLL custInfoBLL, DepoistService depoistService, WithdrawalService withdrawalService)
        {
            this.bankInfoOffBLL = bankInfoOffBLL;
            this.custInfoBLL = custInfoBLL;
            this.depoistService = depoistService;
            this.withdrawalService = withdrawalService;
            this.bankInfoBLL = bankInfoBLL;
        }

        public int DepositReqWireTransfer(int custId, int sysId, decimal amount, string memo, out DepositRep3 depositRep3)
        {
            depositRep3 = new DepositRep3();

            if (amount <= 0)
                return -1;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankAccountOffList = bankInfoOffBLL.GetBankAccountOff(custInfo.SiteId);
            var _info = (from x in bankAccountOffList
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
                             x.AccountNo
                         }).FirstOrDefault();
            if (_info != null)
            {
                DepositDetailInfo depositDetailInfo = new DepositDetailInfo();
                depositDetailInfo.Amount = amount;
                depositDetailInfo.CustId = custId;
                depositDetailInfo.DepositType = (int)DepositType.WireTransfer;
                depositDetailInfo.Memo = memo;
                depositDetailInfo.SiteId = custInfo.SiteId;
                depositDetailInfo.UserName = custInfo.UserName;

                DepositInfo3_1 depositInfo3_1 = new DepositInfo3_1();
                depositInfo3_1.AccountName = _info.AccountName;
                depositInfo3_1.SysId = _info.SysId;
                depositInfo3_1.AccountNo = _info.AccountNo;
                depositInfo3_1.Ver = 1;
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositInfo3_1);
                string transId = null;
                string postScript = null;

                depoistService.DepositReq(depositDetailInfo, out transId, out postScript);
                depositRep3.TransId = transId;
                depositRep3.PostScript = postScript;
                depositRep3.AccountNo = depositInfo3_1.AccountNo;
                depositRep3.AccountName = depositInfo3_1.AccountName;
                depositRep3.BankName = _info.BankName;
                depositRep3.BankLogo = _info.BankLogo;
                depositRep3.BankSite = _info.BankSite;

                return 0;
            }

            var _info1 = (from x in bankAccountOffList
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
        public int DepositReqOnlineBanking(int custId, int channelId, decimal amount,out string transId)
        {
            transId = null;
            if (amount <= 0)
                return -1;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var payCurrency = bankInfoBLL.GetPayCurrency().Where(x => x.CurrencyId == channelId && custInfo.CurrencyId == x.CurrencyId).FirstOrDefault();

            if (payCurrency == null || payCurrency.MaxAmount < amount || payCurrency.MinAmount > amount)
            {
                return -2;
            }

            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var onlineBankList = bankInfoBLL.GetOnlineBankList();
            var _info = onlineBankList.Where(x => vipLevel >= x.PointLevel && x.ChannelId == channelId && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (_info != null)
            {
                DepositDetailInfo depositDetailInfo = new DepositDetailInfo();
                depositDetailInfo.Amount = amount;
                depositDetailInfo.CustId = custId;
                depositDetailInfo.DepositType = (int)DepositType.OnlineBanking;
                depositDetailInfo.Memo = null;
                depositDetailInfo.SiteId = custInfo.SiteId;
                depositDetailInfo.UserName = custInfo.UserName;
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(_info);
                string postScript = null;

                depoistService.DepositReq(depositDetailInfo, out transId, out postScript);
                return 0;
            }

            return -99;
        }
        public int WithdrawalReq(int custId, int sysId, string accountName, string accountNo, decimal amount, string memo, out decimal afterAmount, out string transId)
        {
            afterAmount = 0;
            transId = "";

            if (amount <= 0)
                return -99;

            WithdrawalInfo withdrawalInfo = new WithdrawalInfo();
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);

            withdrawalInfo.AccountName = accountName;
            withdrawalInfo.AccountNo = accountNo;
            withdrawalInfo.SysId = sysId;

            return withdrawalService.WithdrawalReq(custId, CustInfo.UserName, CustInfo.SiteId, amount, memo, withdrawalInfo, out afterAmount, out transId);
        }

        public IEnumerable<DepositInfoHistory> GetDepositInfoRecent(int custId)
        {
            var CustInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var depositInfoRecent = depoistService.GetDepositInfoRecent(custId, CustInfo.SiteId, 5);
            foreach (var depositInfo in depositInfoRecent)
            {
                if (depositInfo.DepositType == 3)
                {
                    depositInfo.DepositRep = Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo3_1>(depositInfo.DepositInfo);
                    var depositInfoRef = (DepositInfo3_1)depositInfo.DepositRep;
                    depositInfo.BankInfo = bankInfoBLL.GetBankInfoSysListByCurrency(CustInfo.CurrencyId).Where(x => x.SysId == depositInfoRef.SysId).FirstOrDefault();
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
            var depositInfoHistory = depoistService.GetDepositInfoHistory(CustInfo.CustId, startDate, endDate, CustInfo.SiteId, 5, ref pageNumber, out totalPages, out rowCount);

            foreach (var depositInfo in depositInfoHistory)
            {
                if (depositInfo.DepositType == 3)
                {
                    depositInfo.DepositRep = Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo3_1>(depositInfo.DepositInfo);
                    var depositInfoRef = (DepositInfo3_1)depositInfo.DepositRep;
                    depositInfo.BankInfo = bankInfoBLL.GetBankInfoSysListByCurrency(CustInfo.CurrencyId).Where(x => x.SysId == depositInfoRef.SysId).FirstOrDefault();
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
    }
}
