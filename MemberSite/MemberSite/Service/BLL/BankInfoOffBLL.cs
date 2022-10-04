using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class BankInfoOffBLL
    {
        private readonly BankInfoOffService _bankInfoService;
        private readonly BankInfoBLL _bankInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositBLL _depositBll;
        private readonly SysBankInfoOffComparer _sysBankInfoOffComparer;
        private readonly DepositService _depositService;
        private readonly ICacheHelper _redisHelper;

        public BankInfoOffBLL(IServiceProvider provider)
        {
            _bankInfoService = provider.GetRequiredService<BankInfoOffService>();
            _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _sysBankInfoOffComparer = new SysBankInfoOffComparer();
            _depositService = provider.GetRequiredService<DepositService>();
        }

        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId, int queryType = 1)
        {
            return _bankInfoService.GetBankAccountOff(siteId, queryType).Where(x => (x.Purpose & 1) == 1);
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

        public IEnumerable<OfflineBankInfoFront> GetBankInfoOffList(int custId, int queryType = 0)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var bankAccountOffList = GetBankAccountOff(custInfo.SiteId, queryType);

            var newbankAccountOffList = JsonSerializer.Deserialize<IEnumerable<BankInfoOff>>(JsonSerializer.Serialize(bankAccountOffList));
            var localBankList = new List<BankInfoOff>(newbankAccountOffList?.ToList());

            var depositLimitInfoList = JsonSerializer.Deserialize<IEnumerable<DepositLimitInfo>>(
                    JsonSerializer.Serialize(_depositBll.GetDepositLimitInfoList(custInfo.SiteId))).ToList();
            var depositLimitInfo = depositLimitInfoList.FirstOrDefault(x =>
                x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.LocalBank &&
                x.CurrencyId == custInfo.CurrencyId);
            var custDepositSysIds = _bankInfoService.GetCustOffDepositSysId(custId);
            
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new OfflineBankInfoFront[] { };
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

            var result =
                _info.Select(x => new OfflineBankInfoFront(x, !custDepositSysIds.Contains(x.SysId)));
            
            return result;
        }

        public IEnumerable<OfflineBankInfoFront> GetBankInfoOffListWithCache(int custId, string lang, int queryType = 0)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var siteId = custInfo.SiteId;
            var currencyId = custInfo.CurrencyId;

            var key = $"GetBankInfoOffList:{siteId}:{currencyId}:{vipLevel}:{lang}:{queryType}";
            var offBankList = _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => GetBankAccountOffList(siteId, currencyId, vipLevel, lang, queryType));

            var custDepositSysIds = _bankInfoService.GetCustOffDepositSysId(custId);
            var result = offBankList
                .Select(x => new OfflineBankInfoFront(x, !custDepositSysIds.Contains(x.SysId)));
            return result;
        }

        public IEnumerable<SysBankInfoFront> GetBankAccountOffList(int siteId, int currencyId, int vipLevel, string lang, int queryType = 0)
        {
            var newbankAccountOffList = _bankInfoService.GetBankAccountOff(siteId, queryType)
                .Where(x => (x.Purpose & 1) == 1) //(x.Purpose & 1):選出x.Purpose為奇數的項目
                .ToList();

            if (siteId == (int) SiteId.Nova88
                && currencyId == (int) CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                BankNameLocalization(newbankAccountOffList, lang);
            }

            var localBankList = new List<BankInfoOff>(newbankAccountOffList);

            var depositLimitInfoList = _depositService.GetDepositLimitInfoList().Where(x => x.SiteId == siteId)?.ToList();
            var depositLimitInfo = depositLimitInfoList.FirstOrDefault(x =>
                x.SiteId == siteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.LocalBank &&
                x.CurrencyId == currencyId);

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

            var _info = localBankList
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == currencyId && x.MaxAmount > 0)
                .OrderBy(x => x.BSort)
                .Select( x => new SysBankInfoFront
                {
                    SysId = x.SysId,
                    BankCode = x.BankCode,
                    BankLogo = x.BankLogo,
                    BankName = x.BankName,
                    IFSCCode = x.IFSCCode,
                    BankSite = x.BankSite,
                    Status = x.Status,
                })
                .Distinct(_sysBankInfoOffComparer).ToList();

            for (var i = 0; i < _info.Count; i++)
            {
                var _info1 = (from x in localBankList
                              where vipLevel >= x.PointLevel && x.CurrencyId == currencyId && _info[i].SysId == x.SysId && x.MaxAmount > 0
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

        private void BankNameLocalization(in List<BankInfoOff> list, string lang)
        {
            var bankNameList = _bankInfoBll.GetBankInfoName(lang);
            if (bankNameList.Any())
            {
                list.ForEach(x => x.BankName =
                    bankNameList.FirstOrDefault(y => y.SysId == x.SysId)?.BankName ?? x.BankName);
            }
        }
    }
}
