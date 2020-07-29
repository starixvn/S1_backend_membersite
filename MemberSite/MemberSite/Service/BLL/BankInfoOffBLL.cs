using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class BankInfoOffBLL
    {
        private readonly IMemoryCache cache;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly BankInfoOffService bankInfoService;
        private readonly CustInfoBLL custInfoBLL;
        private readonly DepositBLL depositBLL;
        private readonly SysBankInfoOffComparer sysBankInfoOffComparer;

        public BankInfoOffBLL(IMemoryCache cache, ServerInfoBLL serverInfoBLL, BankInfoOffService bankInfoService, CustInfoBLL custInfoBLL, DepositBLL depositBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.bankInfoService = bankInfoService;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
            this.depositBLL = depositBLL;
            sysBankInfoOffComparer = new SysBankInfoOffComparer();
        }

        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId)
        {
            string sBankInfoOffListKey = $"BankInfoOffList_{siteId}";
            var BankInfoOffList = cache.Get<IEnumerable<BankInfoOff>>(sBankInfoOffListKey);
            if (BankInfoOffList == null)
            {
                BankInfoOffList = bankInfoService.GetBankAccountOff(siteId);
                if (BankInfoOffList != null)
                {
                    cache.Set(sBankInfoOffListKey, BankInfoOffList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
                }
            }

            return BankInfoOffList.Where(x => (x.Purpose & 1) == 1);
        }

        private class SysBankInfoOffComparer : IEqualityComparer<SysBankInfoFront>
        {
            public bool Equals(SysBankInfoFront t1, SysBankInfoFront t2)
            {
                return (t1.SysId == t2.SysId && t1.BankCode == t2.BankCode);
            }

            public int GetHashCode(SysBankInfoFront t)
            {
                return t.ToString().GetHashCode();
            }
        }

        public IEnumerable<SysBankInfoFront> GetBankInfoOffList(int custId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankAccountOffList = GetBankAccountOff(custInfo.SiteId);

            List<BankInfoOff> localBankList = bankAccountOffList?.ToList();
            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.LocalBank && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new SysBankInfoFront[] { };
            }

            if (depositLimitInfo != null && localBankList != null)
            {
                foreach (var localBack in localBankList)
                {
                    depositBLL.FixDepositLimit(localBack, depositLimitInfo);
                }
            }

            var _info = (from x in localBankList
                         where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.MaxAmount > 0
                         orderby x.PointLevel descending, x.Sort
                         select new SysBankInfoFront
                         {
                             SysId = x.SysId,
                             BankCode = x.BankCode,
                             BankLogo = x.BankLogo,
                             BankName = x.BankName,
                             BankSite = x.BankSite,
                         }).Distinct(sysBankInfoOffComparer).ToList();

            for (var i = 0; i < _info.Count(); i++)
            {
                var _info1 = (from x in localBankList
                              where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && _info[i].SysId == x.SysId && x.MaxAmount > 0
                              select new
                              {
                                  x.MinAmount,
                                  x.MaxAmount
                              }).OrderByDescending(x => x.MaxAmount == 0 ? decimal.MaxValue : x.MaxAmount).ThenBy(x => x.MinAmount).FirstOrDefault();

                _info[i].MinAmount = _info1.MinAmount;
                _info[i].MaxAmount = _info1.MaxAmount;
            }

            //foreach (var bk in _info)
            //{
            //    var _info1 = (from x in bankAccountOffList
            //                  where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && bk.SysId == x.SysId
            //                  select new
            //                  {
            //                      x.MinAmount,
            //                      x.MaxAmount
            //                  }).OrderByDescending(x => x.MaxAmount == 0 ? decimal.MaxValue : x.MaxAmount).ThenBy(x => x.MinAmount).Distinct().FirstOrDefault();

            //    bk.MinAmount = _info1.MinAmount;
            //    bk.MaxAmount = _info1.MaxAmount;
            //}

            return _info;
        }
    }
}
