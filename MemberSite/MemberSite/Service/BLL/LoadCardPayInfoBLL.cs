using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class LoadCardPayInfoBLL
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositLimitBLL _depositLimitBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly PayInfoBLL _payInfoBll;

        public LoadCardPayInfoBLL(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositLimitBll = provider.GetRequiredService<DepositLimitBLL>();
            _payInfoBll = provider.GetRequiredService<PayInfoBLL>();
        }

        public IEnumerable<LoadCardPayGroup> GetLoadCardPayGroupList(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var LoadCardPayInfoList = _payInfoBll.GetPayInfoListWithCache(custInfo.SiteId, DepositType.LoadCardPayment);
            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.LoadCardPayment);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<LoadCardPayGroup>();
            }

            var LoadCardPayGroupList = LoadCardPayInfoList
                .Where(x => pointLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId)
                .GroupBy(x => x.GroupId)
                .Select(x =>
                {
                    var d = x.OrderBy(y => y.SSort).First();

                    return new LoadCardPayGroup
                    {
                        GroupId = d.GroupId,
                        GroupName = d.GroupName,
                        DisplayGroupName = d.DisplayGroupName,
                        PayName = d.PayName,
                        GSort = d.GSort,
                        Mode = d.Mode,
                        MaxAmount = d.MaxAmount,
                        MinAmount = d.MinAmount
                    };
                });

            if (depositLimitInfo != null)
            {
                foreach (var payGroup in LoadCardPayGroupList)
                {
                    _depositLimitBll.FixDepositLimit(payGroup, depositLimitInfo);
                }
            }

            return LoadCardPayGroupList;
        }

        public IEnumerable<PayInfo> GetLoadCardPayInfoList(int custId, int groupId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.LoadCardPayment);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<PayInfo>();
            }

            var LoadCardPayInfoList = _payInfoBll.GetPayInfoListWithCache(custInfo.SiteId, DepositType.LoadCardPayment)
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId).ToList();

            if (depositLimitInfo != null && LoadCardPayInfoList != null)
            {
                foreach (var LoadCardPayInfo in LoadCardPayInfoList)
                {
                    _depositLimitBll.FixDepositLimit(LoadCardPayInfo, depositLimitInfo);
                }
            }

            return LoadCardPayInfoList.Where(x => x.MaxAmount > 0);
        }
    }
}
