using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemberSite.Service.BLL
{
    public class MobilePayInfoBLL
    {
        private readonly PayService payService;
        private readonly DepositService _depositService;
        private readonly DepositBLL depositBLL;
        private readonly IMemoryCache cache;
        private readonly CustInfoBll custInfoBLL;
        private readonly MobilePayGroupComparer mobilePayGroupComparer;
        public MobilePayInfoBLL(IMemoryCache cache, PayService payService, DepositService depositService, CustInfoBll custInfoBLL, DepositBLL depositBLL)
        {
            this.payService = payService;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
            this._depositService = depositService;
            this.depositBLL = depositBLL;
            mobilePayGroupComparer = new MobilePayGroupComparer();
        }

        public IEnumerable<MobilePayInfo> GetMobilePayInfoList(int siteId)
        {
            var key = $"MobilePayInfoList_{siteId}";
            var mobilePayInfoList = cache.Get<IEnumerable<MobilePayInfo>>(key);
            if (mobilePayInfoList == null)
            {
                mobilePayInfoList = payService.GetMobilePayInfo(siteId).OrderBy(x => x.GSort).OrderBy(x => x.SSort);

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
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var mobilePayInfoList = GetMobilePayInfoList(custInfo.SiteId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.MobilePayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new MobilePayGroup[] { };
            }

            var mobilePayGroupList = (from x in mobilePayInfoList
                                      where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId
                                      select new MobilePayGroup
                                      {
                                          GroupId = x.GroupId,
                                          GroupName = x.GroupName,
                                          ChannelName = x.ChannelName,
                                          GSort = x.GSort,
                                          Mode = x.Mode,
                                          MinAmount = (from y in mobilePayInfoList
                                                       where vipLevel >= y.PointLevel && y.CurrencyId == custInfo.CurrencyId && x.ChannelId == y.ChannelId
                                                       select new
                                                       {
                                                           y.MinAmount
                                                       }).OrderBy(y => y.MinAmount).FirstOrDefault().MinAmount,
                                          MaxAmount = (from y in mobilePayInfoList
                                                       where vipLevel >= y.PointLevel && y.CurrencyId == custInfo.CurrencyId && x.ChannelId == y.ChannelId
                                                       select new
                                                       {
                                                           y.MaxAmount
                                                       }).OrderByDescending(y => y.MaxAmount).FirstOrDefault().MaxAmount
                                      }).Distinct(mobilePayGroupComparer);

            return mobilePayGroupList;
        }

        public IEnumerable<MobilePayInfo> GetMobilePayInfoList(int custId, int groupId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.MobilePayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new MobilePayInfo[] { };
            }

            var mobilePayInfoList = GetMobilePayInfoList(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId).ToList();

            if (depositLimitInfo != null && mobilePayInfoList != null)
            {
                foreach (var mobilePayInfo in mobilePayInfoList)
                {
                    depositBLL.FixDepositLimit(mobilePayInfo, depositLimitInfo);
                }
            }

            return mobilePayInfoList.Where(x => x.MaxAmount > 0);
        }
        public int DepositReqMobilePay(int custId, int channelId, decimal amount, out string transId, out MobilePayInfo mobilePayInfo)
        {
            transId = null;
            mobilePayInfo = null;
            if (amount <= 0)
                return -1;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.MobilePayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            mobilePayInfo = GetMobilePayInfoList(custInfo.SiteId).FirstOrDefault(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId);

            if (depositLimitInfo != null && mobilePayInfo != null)
            {
                depositBLL.FixDepositLimit(mobilePayInfo, depositLimitInfo);
            }

            if (mobilePayInfo == null || mobilePayInfo.MaxAmount < amount || mobilePayInfo.MinAmount > amount)
            {
                return -2;
            }
            else if (mobilePayInfo.AmountRule == 3 && !mobilePayInfo.AmountList.Contains(amount))
            {
                return -2;
            }

            if (mobilePayInfo != null)
            {
                var depositDetailInfo = new DepositDetailInfo
                {
                    Amount = amount,
                    CustId = custId,
                    DepositType = (int)DepositType.MobilePayment,
                    Memo = null,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId,
                    DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(mobilePayInfo)
                };

                _depositService.DepositReq(depositDetailInfo, out transId, out _);
                return 0;
            }

            return -99;
        }
    }
}
