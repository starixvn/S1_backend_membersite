using MemberSite.Extensions;
using MemberSite.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        public DateTime CreateTime { set; get; }
        public int Status { set; get; }
        public string Memo { set; get; }
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
                var date = utcNow.Date.ToShortDateString();
                var begin = DateTime.ParseExact($"{date} {BeginTime}", "yyyy/M/dd HH:mm", null);
                var end = DateTime.ParseExact($"{date} {EndTime}", "yyyy/M/dd HH:mm", null);

                if (MaintainType == MaintainType.Day)
                {
                    return utcNow.IsBetween(begin, end);
                }

                if (MaintainType == MaintainType.Week && (int)utcNow.DayOfWeek == TypeValue)
                {
                    return utcNow.IsBetween(begin, end);
                }

                if (MaintainType == MaintainType.Month && utcNow.Day == TypeValue)
                {
                    return utcNow.IsBetween(begin, end);
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
