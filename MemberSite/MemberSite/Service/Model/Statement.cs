using MemberSite.Extensions;
using System.Text.Json.Serialization;

namespace MemberSite.Service.Model
{
    public class Statement
    {
        public long TransId { get; set; }
        public string Type { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal BeforeAmount { get; set; }
        public decimal AfterAmount { get; set; }
        public string RefTransId { get; set; }
        public DateTimeOffset CreateTime
        {
            get
            {
                if (CreateTimeOffset != null)//從redis拿的cache資料已經位移過，不需要時區位移
                {
                    return CreateTimeOffset.Value;
                }

                //先用目前時間組合取得時區來進行時間位移
                var offset = DateTime.Now.ToDateTimeOffset(CustTimeZone).Offset;
                CreateTimeOffset = new DateTimeOffset(_createTime).ToOffset(offset);
                return CreateTimeOffset.Value;
            }
            set
            {
                _createTime = value.DateTime;
            }
        }
        public DateTimeOffset? CreateTimeOffset { get; set; } = null;

        private DateTime _createTime { get; set; }

        public int Direction { get; set; }

        public string TypeName { get; set; }

        public string CustTimeZone { get; set; }
    }

    public class TpList
    {
        public int Id { get; set; }
        public string TpName { get; set; }
        public bool Fun { get; set; }
    }

    public class TpBetRecordListOneDay
    {
        public DateTimeOffset SettledDate
        {
            get
            {
                return _settledDate.ToDateTimeOffset(CustTimeZone);
            }
            set
            {
                _settledDate = value.DateTime;
            }
        }
        private DateTime _settledDate { get; set; }
        public string CategoryName { get; set; }
        public int CategoryId { get; set; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
        [JsonIgnore]
        public string CustTimeZone { get; set; }
    }
    public class TpBetRecordbyCategoryId
    {
        public DateTimeOffset SettledDate
        {
            get
            {
                return _settledDate.ToDateTimeOffset(CustTimeZone);
            }
            set
            {
                _settledDate = value.DateTime;
            }
        }
        private DateTime _settledDate { get; set; }
        public string GameTypeName { get; set; }
        public int GameTypeID { get; set; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
        [JsonIgnore]
        public string CustTimeZone { get; set; }
    }
    public class TpBetRecordDetailbyGameType
    {
        public string TransId { get; set; }
        public int BonusId { get; set; }
        public string BonusCode { set; get; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
        public DateTimeOffset TransTime
        {
            get
            {
                return _transTime.ToDateTimeOffset(CustTimeZone);
            }
            set
            {
                _transTime = value.DateTime;
            }
        }
        private DateTime _transTime { get; set; }
        public DateTimeOffset SettledTime
        {
            get
            {
                return _settledTime.ToDateTimeOffset(CustTimeZone);
            }
            set
            {
                _settledTime = value.DateTime;
            }
        }
        private DateTime _settledTime { get; set; }

        [JsonIgnore]
        public string CustTimeZone { get; set; }
    }
    public class TpBetRecordListDateRange
    {
        public DateTimeOffset SettledDate
        {
            get
            {
                return _settledDate.ToDateTimeOffset(CustTimeZone);
            }
            set
            {
                _settledDate = value.DateTime;
            }
        }
        private DateTime _settledDate { get; set; }
        public decimal TurnOver { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
        [JsonIgnore]
        public string CustTimeZone { get; set; }
    }
    public class GameTypeList
    {
        public int Type { get; set; }
        public string TypeName { get; set; }
    }

    public class CryptoInfo
    {
        public int CurrencyId { get; set; }

        public string CurrencyName { get; set; }

        public string Network { get; set; }
    }

    public class CryptoInfoData
    {
        /// <summary>
        /// 加密貨幣幣別
        /// </summary>
        public IEnumerable<CryptoCurrency> CryptoCurrency { get; set; }

        /// <summary>
        /// 加密貨幣協定
        /// </summary>
        public IEnumerable<CryptoNetwork> CryptoNetwork { get; set; }
    }

    public class CryptoCurrency
    {
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }
    }

    public class CryptoNetwork
    {
        public int CurrencyId { get; set; }
        public string Network { get; set; }
    }
}
