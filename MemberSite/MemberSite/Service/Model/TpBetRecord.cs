namespace MemberSite.Service.Model;

public class TpBetRecord
{
    public int TpId { get; set; }
    public string TransId { get; set; }
    public int GameType { get; set; }
    public int CustId { get; set; }
    public string UserName { get; set; }
    public int CurrencyId { get; set; }
    public int SiteId { get; set; }
    public string TpUserName { get; set; }
    public DateTimeOffset TransTime
    {
        get
        {
            return new DateTimeOffset(_transTime, TimeSpan.Zero);
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
            return new DateTimeOffset(_settledTime, TimeSpan.Zero);
        }
        set
        {
            _settledTime = value.DateTime;
        }

    }
    private DateTime _settledTime { get; set; }
    public decimal Stake { get; set; }
    public decimal? ValidBet { get; set; }
    public decimal? Revenue { get; set; }
    public decimal WinlostAmount { get; set; }
    public decimal ProgressiveBet { get; set; }
    public decimal ProgressiveWin { get; set; }
    public int SettlementType { get; set; }
    public int TicketStatus { get; set; }
    public string OriginalState { get; set; }
    public long TicketKey { get; set; }
    public int BonusId { get; set; }
    public DateTimeOffset CreateTime
    {
        get
        {
            return new DateTimeOffset(_createTime, TimeSpan.Zero);
        }
        set
        {
            _createTime = value.DateTime;
        }

    }
    private DateTime _createTime { get; set; }
}