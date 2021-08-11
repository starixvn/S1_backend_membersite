using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class LoadCardPayInfoBLL
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly DepositBLL depositBLL;
        private readonly PayService payService;
        private readonly DepoistService depoistService;
        private readonly IMemoryCache cache;
        private readonly CourierAdapter courierAdapter;
        private readonly string payApiUrl;
        private readonly LoadCardPayGroupComparer loadCardGroupComparer;
        public LoadCardPayInfoBLL(IMemoryCache cache, IConfiguration configuration, PayService payService, DepoistService depoistService, CustInfoBLL custInfoBLL, DepositBLL depositBLL, CourierAdapter courierAdapter)
        {
            this.custInfoBLL = custInfoBLL;
            this.depositBLL = depositBLL;
            this.payService = payService;
            this.depoistService = depoistService;
            this.cache = cache;
            payApiUrl = configuration.GetValue<string>("PayService");
            this.courierAdapter = courierAdapter;
            loadCardGroupComparer = new LoadCardPayGroupComparer();
        }
        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int siteId)
        {
            string key = $"LoadCardPayInfoList_{siteId}";
            var LoadCardPayInfoList = cache.Get<IEnumerable<LoadCardPayInfo>>(key);
            if (LoadCardPayInfoList == null)
            {
                LoadCardPayInfoList = payService.GetLoadCardPayInfo(siteId).OrderBy(x => x.GSort).OrderBy(x => x.SSort);

                if (LoadCardPayInfoList != null)
                {
                    cache.Set(key, LoadCardPayInfoList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return LoadCardPayInfoList;
        }
        private class LoadCardPayGroupComparer : IEqualityComparer<LoadCardPayGroup>
        {
            public bool Equals(LoadCardPayGroup t1, LoadCardPayGroup t2)
            {
                return (t1.GroupId == t2.GroupId);
            }

            public int GetHashCode(LoadCardPayGroup t)
            {
                return t.ToString().GetHashCode();
            }
        }
        public IEnumerable<LoadCardPayGroup> GetLoadCardPayGroupList(int custId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var LoadCardPayInfoList = GetLoadCardPayInfoList(custInfo.SiteId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.MobilePayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new LoadCardPayGroup[] { };
            }

            var LoadCardPayGroupList = (from x in LoadCardPayInfoList
                                      where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId
                                      select new LoadCardPayGroup
                                      {
                                          GroupId = x.GroupId,
                                          GroupName = x.GroupName,
                                          PayName = x.PayName,
                                          GSort = x.GSort,
                                          Mode = x.Mode
                                      }).Distinct(loadCardGroupComparer);

            return LoadCardPayGroupList;
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int custId, int groupId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.MobilePayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new LoadCardPayInfo[] { };
            }

            var LoadCardPayInfoList = GetLoadCardPayInfoList(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId).ToList();

            if (depositLimitInfo != null && LoadCardPayInfoList != null)
            {
                foreach (var LoadCardPayInfo in LoadCardPayInfoList)
                {
                    depositBLL.FixDepositLimit(LoadCardPayInfo, depositLimitInfo);
                }
            }

            return LoadCardPayInfoList.Where(x => x.MaxAmount > 0);
        }
        public int DepositReqLoadCardPay(int custId, int channelId, string code, string serial, decimal amount, out string transId, out LoadCardPayInfo loadCardPayInfo)
        {
            transId = null;
            loadCardPayInfo = null;
            if (amount <= 0)
                return -1;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.LoadCardPayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            loadCardPayInfo = GetLoadCardPayInfoList(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId).FirstOrDefault();

            if (depositLimitInfo != null && loadCardPayInfo != null)
            {
                depositBLL.FixDepositLimit(loadCardPayInfo, depositLimitInfo);
            }

            if (loadCardPayInfo == null || loadCardPayInfo.MaxAmount < amount || loadCardPayInfo.MinAmount > amount)
            {
                return -2;
            }
            else if (loadCardPayInfo.AmountRule == 3 && !loadCardPayInfo.AmountList.Contains(amount))
            {
                return -2;
            }

            if (loadCardPayInfo != null)
            {
                loadCardPayInfo.Code = code;
                loadCardPayInfo.Serial = serial;

                DepositDetailInfo depositDetailInfo = new DepositDetailInfo();
                depositDetailInfo.Amount = amount;
                depositDetailInfo.CustId = custId;
                depositDetailInfo.DepositType = (int)DepositType.LoadCardPayment;
                depositDetailInfo.Memo = null;
                depositDetailInfo.SiteId = custInfo.SiteId;
                depositDetailInfo.UserName = custInfo.UserName;
                depositDetailInfo.CurrencyId = custInfo.CurrencyId;
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(loadCardPayInfo);
                string postScript = null;

                depoistService.DepositReq(depositDetailInfo, out transId, out postScript);
                return 0;
            }

            return -99;
        }
    }
}
