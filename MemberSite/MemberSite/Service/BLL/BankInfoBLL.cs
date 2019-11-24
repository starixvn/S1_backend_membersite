using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class BankInfoBLL
    {
        private readonly IMemoryCache cache;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly BankInfoService bankInfoService;
        private readonly CustInfoBLL custInfoBLL;

        public BankInfoBLL(IMemoryCache cache, ServerInfoBLL serverInfoBLL, BankInfoService bankInfoService, CustInfoBLL custInfoBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.bankInfoService = bankInfoService;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
        }

        public IEnumerable<BankInfoOff> GetBankAccountOff()
        {
            var serverInfo = serverInfoBLL.GetServerInfo();

            string sBankInfoOffListKey = $"BankInfoOffList_{serverInfo.SiteId}";
            var BankInfoOffList = cache.Get<IEnumerable<BankInfoOff>>(sBankInfoOffListKey);
            if (BankInfoOffList != null)
                return BankInfoOffList;

            BankInfoOffList = bankInfoService.GetBankAccountOff(serverInfo.SiteId);
            cache.Set(sBankInfoOffListKey, BankInfoOffList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(2)));
            return BankInfoOffList;
        }

        public IEnumerable<SysBankInfo> GetBankInfoList(int custId)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankAccountOffList = GetBankAccountOff();
            var _info = (from x in bankAccountOffList
                         where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId
                         select new SysBankInfo
                         {
                             BankId = x.BankId,
                             BankLogo = x.BankLogo,
                             BankName = x.BankName,
                             BankSite = x.BankSite,
                             Sort = x.Sort,
                             SysId = x.SysId
                         }).OrderBy(x => x.Sort).Distinct();
            return _info;
        }
    }
}
