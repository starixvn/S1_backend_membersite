using System.Data;

using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IEventsService
    {
        IEnumerable<Prize> CheckDrawPrize(ref int eventId, int custId, int currencyId, out int prizeId, out int quota, out DateTime checkDate, out int result, IDbTransaction trans);
        bool CheckEventCondition(string conditionSP, int eventId, int custId, int currencyId);
        int DrawPrize(ref int eventId, int custId, int currencyId, string userName, int siteId, out int result, out Prize drawPrize);
        IEnumerable<CustGiftList> GetCustDrawPrizeList(int eventId, int siteId, int custId);
        Task<IEnumerable<CustGiftList>> GetTodayCustDrawPrizeList(int eventId, int custId, int currencyId);
        IEnumerable<PrizeDisPlayList> GetDrawPrizeDisPlaySetting(int eventId);
        IEnumerable<PrizeList> GetDrawPrizeList(int eventId, int currencyId);
        int GetDrawPrizeMessage(int eventId, string langId, out string tital, out string message);
        EventDrawInfo GetEventDrawInfo(int eventId);
        Task<IEnumerable<Prize>> GetPrizes(int eventId, int currencyId);
        Task ResetPrize(int eventId, int currencyId);
        Task<IEnumerable<AngPowTicket>> GetTickets(int custId, int currencyId);
        Task<Prize> GetCustPrizeOnToday(int eventId, int custId, int currencyId);
        Task<int> PayoutPrize(int eventId, int prizeId, string prizeName, int siteId, int custId, string userName, int currencyId, decimal prizeAmount, int ticketNo, int prizeType, bool isUsed, bool isBigPrize);

        #region PromotionMission

        public IEnumerable<PromotionMission> GetPromotionMission(int siteId, int currencyId, string missionDate);
        public PromotionMissionProgress GetCustPromotionMissionProgress(int custId, int currencyId, int tpCategoryId, DateTime startTime, DateTime endTime);
        public IEnumerable<PromotionMissionMessage> GetPromotionMissionMessage();
        public IEnumerable<CustPromotionMission> GetCustPromotionMission(int custId, string missionDate);
        public int ClaimPromotionMission(int custId, int siteId, int currencyId, string missionDate, IEnumerable<NewYearMission2024> mission);
        public int GetEggTicket(int custId);
        public int GetCustBlessingsSticker(int custId);
        public IEnumerable<EggTicketData> GetEggTicketData(int custId);

        #endregion
        
        #region Olympcic

        public IEnumerable<OlympicEventSBSWModel> GetOlympicsEventSportBet(int sq,int custId,string transList);

        public int OlympicsInertGoldMetal(int custId, int goldMetalCount,string type);
        public int OlympicsEventGoldMetalSet(int id,int custId ,string goldMetalIdList);
        public bool CreateAdminAdjustmentInfoLog(CreateAdminAdjustmentInfoLogInput input);
        public int OlympicsEventDrawLogIns(int eventId, int prizeId, string prizeName, decimal amount, int custId,
            int currencyId);

        #endregion

    }
}
