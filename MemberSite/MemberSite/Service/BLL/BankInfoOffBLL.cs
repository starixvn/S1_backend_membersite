using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MemberSite.Service.Library;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.BLL
{
    public class BankInfoOffBLL
    {
        private readonly BankInfoOffService _bankInfoService;
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositBLL _depositBll;
        private readonly SysBankInfoOffComparer _sysBankInfoOffComparer;
        private readonly DepositService _depositService;
        private readonly CacheHelper _cacheHelper;

        public BankInfoOffBLL(IServiceProvider provider)
        {
            _bankInfoService = provider.GetRequiredService<BankInfoOffService>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _sysBankInfoOffComparer = new SysBankInfoOffComparer();
            _depositService = provider.GetRequiredService<DepositService>();
        }

        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId, int queryType = 1)
        {
            var key = $"BankInfoOffList_{siteId}_{queryType}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)),
                i => _bankInfoService.GetBankAccountOff(siteId, queryType).Where(x => (x.Purpose & 1) == 1));
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

        public IEnumerable<SysBankInfoFront> GetBankInfoOffList(int custId, int queryType = 0)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var bankAccountOffList = GetBankAccountOff(custInfo.SiteId, queryType);
            
            var newbankAccountOffList = JsonSerializer.Deserialize<IEnumerable<BankInfoOff>>(JsonSerializer.Serialize(bankAccountOffList));
            var localBankList = new List<BankInfoOff>(newbankAccountOffList?.ToList());

            var depositLimitInfoList = JsonSerializer.Deserialize<IEnumerable<DepositLimitInfo>>(
                    JsonSerializer.Serialize(_depositBll.GetDepositLimitInfoList(custInfo.SiteId))).ToList();
            var depositLimitInfo = depositLimitInfoList.FirstOrDefault(x =>
                x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int) DepositType.LocalBank &&
                x.CurrencyId == custInfo.CurrencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new SysBankInfoFront[] { };
            }

            if (depositLimitInfo != null && localBankList.Any())
            {
                foreach (var localBack in localBankList)
                {
                    _depositBll.FixDepositLimit(localBack, depositLimitInfo);
                }
            }

            var _info = (from x in localBankList
                         where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.MaxAmount > 0
                         orderby x.PointLevel descending, x.BSort
                         select new SysBankInfoFront
                         {
                             SysId = x.SysId,
                             BankCode = x.BankCode,
                             BankLogo = x.BankLogo,
                             BankName = x.BankName,
                             IFSCCode = x.IFSCCode,
                             BankSite = x.BankSite,
                             Status = x.Status,
                         }).Distinct(_sysBankInfoOffComparer).ToList();

            for (var i = 0; i < _info.Count; i++)
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

            return _info;
        }

        public IEnumerable<SysBankInfoFront> GetBankInfoOffListWithCache(int custId, int queryType = 0)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            int SiteId = custInfo.SiteId;
            int CurrencyId = custInfo.CurrencyId;

            var key = $"GetBankInfoOffList_{SiteId}_{CurrencyId}_{vipLevel}_{queryType}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)),
                i => GetBankAccountOffList( SiteId,  CurrencyId, vipLevel, queryType = 0));
        }

        public IEnumerable<SysBankInfoFront> GetBankAccountOffList(int SiteId, int CurrencyId, int vipLevel,  int queryType = 0)
        {

            var newbankAccountOffList = _bankInfoService.GetBankAccountOff(SiteId, queryType).Where(x => (x.Purpose & 1) == 1);//(x.Purpose & 1):選出x.Purpose為奇數的項目
            var localBankList = new List<BankInfoOff>(newbankAccountOffList?.ToList());

            var depositLimitInfoList = (_depositService.GetDepositLimitInfoList().Where(x => x.SiteId == SiteId))?.ToList();
            var depositLimitInfo = depositLimitInfoList.FirstOrDefault(x =>
                x.SiteId == SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.LocalBank &&
                x.CurrencyId == CurrencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new SysBankInfoFront[] { };
            }

            if (depositLimitInfo != null && localBankList.Any())
            {
                foreach (var localBack in localBankList)
                {
                    _depositBll.FixDepositLimit(localBack, depositLimitInfo);
                }
            }

            var _info = (from x in localBankList
                         where vipLevel >= x.PointLevel && x.CurrencyId == CurrencyId && x.MaxAmount > 0
                         orderby x.PointLevel descending, x.BSort
                         select new SysBankInfoFront
                         {
                             SysId = x.SysId,
                             BankCode = x.BankCode,
                             BankLogo = x.BankLogo,
                             BankName = x.BankName,
                             IFSCCode = x.IFSCCode,
                             BankSite = x.BankSite,
                             Status = x.Status,
                         }).Distinct(_sysBankInfoOffComparer).ToList();

            for (var i = 0; i < _info.Count; i++)
            {
                var _info1 = (from x in localBankList
                              where vipLevel >= x.PointLevel && x.CurrencyId == CurrencyId && _info[i].SysId == x.SysId && x.MaxAmount > 0
                              select new
                              {
                                  x.MinAmount,
                                  x.MaxAmount
                              }).OrderByDescending(x => x.MaxAmount == 0 ? decimal.MaxValue : x.MaxAmount).ThenBy(x => x.MinAmount).FirstOrDefault();

                _info[i].MinAmount = _info1.MinAmount;
                _info[i].MaxAmount = _info1.MaxAmount;
            }

            return _info;
        }
    }
}
