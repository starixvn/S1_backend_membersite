using MemberSite.Service.DAL;
using MemberSite.Service.Model;
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
        private readonly SysBankInfoOffComparer sysBankInfoOffComparer;

        public BankInfoOffBLL(IMemoryCache cache, ServerInfoBLL serverInfoBLL, BankInfoOffService bankInfoService, CustInfoBLL custInfoBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.bankInfoService = bankInfoService;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
            sysBankInfoOffComparer = new SysBankInfoOffComparer();
        }

        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId)
        {
            string sBankInfoOffListKey = $"BankInfoOffList_{siteId}";
            var BankInfoOffList = cache.Get<IEnumerable<BankInfoOff>>(sBankInfoOffListKey);
            if (BankInfoOffList != null)
                return BankInfoOffList;

            BankInfoOffList = bankInfoService.GetBankAccountOff(siteId);
            cache.Set(sBankInfoOffListKey, BankInfoOffList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(2)));
            return BankInfoOffList;
        }

        private class SysBankInfoOffComparer : IEqualityComparer<SysBankInfoOff>
        {
            public bool Equals(SysBankInfoOff t1, SysBankInfoOff t2)
            {
                return (t1.SysId == t2.SysId && t1.BankId == t2.BankId);
            }

            public int GetHashCode(SysBankInfoOff t)
            {
                return t.ToString().GetHashCode();
            }
        }

        public IEnumerable<SysBankInfoOff> GetBankInfoOffList(int custId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankAccountOffList = GetBankAccountOff(custInfo.SiteId);
            var _info = (from x in bankAccountOffList
                         where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId
                         orderby x.PointLevel descending, x.Sort
                         select new SysBankInfoOff
                         {
                             SysId = x.SysId,
                             BankId = x.BankId,
                             BankLogo = x.BankLogo,
                             BankName = x.BankName,
                             BankSite = x.BankSite,
                         }).Distinct(sysBankInfoOffComparer).ToList();

            for (var i = 0; i < _info.Count(); i++)
            {
                var _info1 = (from x in bankAccountOffList
                              where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && _info[i].SysId == x.SysId
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
