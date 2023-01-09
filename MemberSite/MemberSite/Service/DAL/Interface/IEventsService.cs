using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IEventsService
    {
        IEnumerable<Prize> CheckDrawPrize(ref int eventId, int siteId, int custId, int currencyId, out int prizeId, out int result);
        bool CheckEventCondition(string conditionSP, int eventId, int custId, int currencyId);
        int DrawPrize(ref int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize);
        IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId);
        Task<IEnumerable<CustGiftList>> GetTodayCustDrawPrizeList(int eventId, int custId, int currencyId);
        void GetCustRank(int lBCategoryId, int custId, int beforeDays, out int custRank, out decimal point);
        void GetCustRankMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays, out int custRank, out decimal point);
        void GetCustRankV3(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out decimal point);
        IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlaySetting(int eventId);
        IEnumerable<PrizeList> GetDrawPrizeList(int eventId, int currencyId);
        int GetDrawPrizeMessage(int eventId, string langId, out string tital, out string message);
        EventDrawInfo GetEventDrawInfo(int eventId);
        void GetLeaderboardEventCode(int siteId, out string eventCodeNow, out string eventCodeLast);
        LeaderboardInfo GetLeaderboardInfo(string eventCode, int siteId, int currencyId);
        IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays);
        IEnumerable<Leaderboard> GetLeaderboardListMonth(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, DateTime selectDays);
        IEnumerable<Leaderboard> GetLeaderboardListV3(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays);
        Task<IEnumerable<Prize>> GetPrizes(int eventId, int currencyId);
        Task<IEnumerable<AngPowTicket>> GetTickets(int custId, int currencyId);
        Task<Prize> GetCustPrizeOnToday(int eventId, int custId, int currencyId);
        Task<int> PayoutPrize(int eventId, int prizeId, string prizeName, int siteId, int custId, string userName, int currencyId, decimal prizeAmount, int ticketNo, int prizeType);
    }
}