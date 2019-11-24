using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class PaymentBLL
    {
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly BankInfoBLL bankInfoBLL;
        private readonly CustInfoBLL custInfoBLL;

        public PaymentBLL(ServerInfoBLL serverInfoBLL, BankInfoBLL bankInfoBLL, CustInfoBLL custInfoBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.bankInfoBLL = bankInfoBLL;
            this.custInfoBLL = custInfoBLL;
        }

        public int DepositReq(int custId, int sysId, decimal amount)
        {
            if (amount <= 0)
                return -1;
            var serverInfo = serverInfoBLL.GetServerInfo();
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankAccountOffList = bankInfoBLL.GetBankAccountOff();
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
                return -3;
            }

            return 0;
        }
    }
}
