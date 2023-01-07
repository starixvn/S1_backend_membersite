using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface
{
    public interface IEventsBLL
    {
        IEnumerable<Prize> CheckDrawPrize(ref int eventId, int siteId, int custId, int currencyId, out int prizeId, out int result);
        Task<(int errorCode, int ticketCount)> CheckDrawPrize(int custId, int currencyId);
        bool CheckEventCondition(string conditionSp, int eventId, int custId, int currencyId);
        int DrawPrize(ref int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize);
        Task<(int error, Prize prize)> DrawPrize(int eventId, int custId, int currencyId, string userName, int siteId);
        IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId);
        LeaderboardCustRank GetCustRank(int lBCategoryId, int custId, int beforeDays);
        LeaderboardCustRank GetCustRankMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays);
        LeaderboardCustRank GetCustRankV3(int lBCategoryId, string eventCode, int custId, int beforeDays);
        void GetCustRankWithCache(int lBCategoryId, int custId, int beforeDays, out int custRank, out decimal point);
        void GetCustRankWithCacheMonth(int lBCategoryId, string eventCode, int custId, DateTime selectDays, out int custRank, out decimal point);
        void GetCustRankWithCacheV2(int lBCategoryId, string eventCode, int custId, int beforeDays, out int custRank, out decimal point);
        IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlayList(int eventId, int currencyId);
        IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlayListWithCache(int eventId, int currencyId);
        DrawPrizeMessage GetDrawPrizeMessage(int eventId, string langId);
        DrawPrizeMessage GetDrawPrizeMessageWithCache(int eventId, string langId);
        EventDrawInfo GetEventDrawInfo(int eventId);
        EventDrawInfo GetEventDrawInfoWithCache(int eventId);
        void GetLeaderboardEventCode(int siteId, out string eventCodeNow, out string eventCodeLast);
        LeaderboardInfo GetLeaderboardInfo(string eventCode, int siteId, int currencyId);
        LeaderboardInfo GetLeaderboardInfoWithCache(string eventCode, int siteId, int currencyId);
        IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays);
        IEnumerable<Leaderboard> GetLeaderboardListMonth(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, DateTime selectDays);
        IEnumerable<Leaderboard> GetLeaderboardListMonthWithCache(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, DateTime selectDays);
        IEnumerable<Leaderboard> GetLeaderboardListV3(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays);
        IEnumerable<Leaderboard> GetLeaderboardListV4(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, int beforeDays);
        IEnumerable<Leaderboard> GetLeaderboardListWithCache(int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays);
        IEnumerable<Leaderboard> GetLeaderboardListWithCacheV2(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays);
        IEnumerable<Leaderboard> GetLeaderboardListWithCacheV3(string eventCode, int lBCategoryId, int siteId, int currencyId, int topNum, int beforeDays);
        Task<IEnumerable<AngPowTicket>> GetTickets(int custId, int currencyId);
        string MaskName(string input);
    }
}