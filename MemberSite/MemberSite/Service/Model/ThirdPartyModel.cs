using System.Text.Json.Serialization;

using MemberSite.Extensions;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model
{
    public class ThirdPartyModel
    {
        public int Id { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public int WalletType { set; get; }
        public int Status { set; get; }
    }
    public class TpCurrency
    {
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
    }

    public class TpTransferInfoHistory
    {
        [JsonIgnore]
        public int TpId { set; get; }
        public string TpName { set; get; }
        public string TransId { set; get; }
        public decimal Amount { set; get; }
        public int Direction { set; get; }
        public DateTimeOffset CreateTime
        {
            get
            {
                return _createTime.ToDateTimeOffset(CustTimeZone);
            }
            set
            {
                _createTime = value.DateTime;
            }
        }
        private DateTime _createTime { get; set; }
        public int Status { set; get; }
        public string Memo { set; get; }
        public bool IsAdjustment { set; get; }

        [JsonIgnore]
        public string CustTimeZone { get; set; }
    }
    public class UserBalance
    {
        public decimal balance { get; set; }
        public decimal bonus { get; set; }
        public decimal outstanding { get; set; }
        public byte Currency { get; set; }
    }

    public class TpMaintain
    {
        public int SiteId { get; set; }
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public int TpCategoryId { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public MaintainType MaintainType { get; set; }
        public int TypeValue { get; set; }
        public string BeginTime { get; set; }
        public string EndTime { get; set; }

        public bool MaintainCheck(DateTime utcNow)
        {
            try
            {
                if (MaintainType == MaintainType.Date)
                {
                    return utcNow.IsBetween(BeginDate, EndDate);
                }

                // 檢查例行性維護是否還在有效區間內                          
                if (!utcNow.IsBetween(BeginDate, EndDate))
                {
                    return false;
                }

                // 將當下日期與維護時間組合後進行檢查
                var date = utcNow.Date.ToString("yyyy/MM/dd");
                var begin = DateTime.ParseExact($"{date} {BeginTime}", "yyyy/MM/dd HH:mm", null);
                var end = DateTime.ParseExact($"{date} {EndTime}", "yyyy/MM/dd HH:mm", null);
                var isBetween = utcNow.IsBetween(begin, end);

                return MaintainType switch
                {
                    MaintainType.Day => isBetween,
                    MaintainType.Week => isBetween && (int)utcNow.DayOfWeek == TypeValue,
                    MaintainType.Month => isBetween && utcNow.Day == TypeValue,
                    _ => false,
                };
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
