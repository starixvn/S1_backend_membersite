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
    public class MobilePayInfoBLL
    {
        private readonly PayService payService;
        private readonly DepoistService depoistService;
        private readonly IMemoryCache cache;
        private readonly CustInfoBLL custInfoBLL;
        private readonly MobilePayGroupComparer mobilePayGroupComparer;
        public MobilePayInfoBLL(IMemoryCache cache, PayService payService, DepoistService depoistService, CustInfoBLL custInfoBLL)
        {
            this.payService = payService;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
            this.depoistService = depoistService;
            mobilePayGroupComparer = new MobilePayGroupComparer();
        }

        public IEnumerable<MobilePayInfo> GetMobilePayInfoList()
        {
            string key = $"MobilePayInfoList";
            var mobilePayInfoList = cache.Get<IEnumerable<MobilePayInfo>>(key);
            if (mobilePayInfoList == null)
            {
                mobilePayInfoList = payService.GetMobilePayInfo().OrderBy(x => x.GSort).OrderBy(x => x.SSort);

                if (mobilePayInfoList != null)
                {
                    cache.Set(key, mobilePayInfoList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return mobilePayInfoList;
        }
        private class MobilePayGroupComparer : IEqualityComparer<MobilePayGroup>
        {
            public bool Equals(MobilePayGroup t1, MobilePayGroup t2)
            {
                return (t1.GroupId == t2.GroupId);
            }

            public int GetHashCode(MobilePayGroup t)
            {
                return t.ToString().GetHashCode();
            }
        }
        public IEnumerable<MobilePayGroup> GetMobilePayGroupList(int custId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var mobilePayInfoList = GetMobilePayInfoList();


            var mobilePayGroupList = (from x in mobilePayInfoList
                                      where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId
                                      select new MobilePayGroup
                                      {
                                          GroupId = x.GroupId,
                                          GroupName = x.GroupName,
                                          GSort = x.GSort
                                      }).Distinct(mobilePayGroupComparer);

            return mobilePayGroupList;
        }

        public IEnumerable<MobilePayInfo> GetMobilePayInfoList(int custId, int groupId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var mobilePayInfoList = GetMobilePayInfoList().Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId);

            return mobilePayInfoList;
        }
        public int DepositReqMobilePay(int custId, int channelId, decimal amount, out string transId, out MobilePayInfo mobilePayInfo)
        {
            transId = null;
            mobilePayInfo = null;
            if (amount <= 0)
                return -1;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            mobilePayInfo = GetMobilePayInfoList().Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId).FirstOrDefault();

            if (mobilePayInfo == null || mobilePayInfo.MaxAmount < amount || mobilePayInfo.MinAmount > amount)
            {
                return -2;
            }
            else if(mobilePayInfo.AmountRule==3 && !mobilePayInfo.AmountList.Contains(amount))
            {
                return -2;
            }

            if (mobilePayInfo != null)
            {
                DepositDetailInfo depositDetailInfo = new DepositDetailInfo();
                depositDetailInfo.Amount = amount;
                depositDetailInfo.CustId = custId;
                depositDetailInfo.DepositType = (int)DepositType.MobilePayment;
                depositDetailInfo.Memo = null;
                depositDetailInfo.SiteId = custInfo.SiteId;
                depositDetailInfo.UserName = custInfo.UserName;
                depositDetailInfo.CurrencyId = custInfo.CurrencyId;
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(mobilePayInfo);
                string postScript = null;

                depoistService.DepositReq(depositDetailInfo, out transId, out postScript);
                return 0;
            }

            return -99;
        }
    }
}
