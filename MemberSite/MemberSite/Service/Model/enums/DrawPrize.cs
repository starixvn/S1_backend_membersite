namespace MemberSite.Service.Model.enums
{
    public enum CupEventNameEnum
    {
        EuroCup2024 = 1,
        ParisOlympic2024 = 2,
    }

    public enum PromotionMissionTypeEnum
    {
        AccumulateDeposit = 1,
        AccumulateValidTurnover = 2,
        SuccessfullyReferAFriend = 3
    }

    public enum DrawPrizeTypeEnum
    {
        NormalPrize = 0,
        NoPrize = 1,
        BlessingsSticker = 2
    }
    
    public enum LotteryTicketEnum
    {
        All = 0,
        Ticket = 1,
        Sticker = 2
    }
    
    public enum LotteryEventDrawEnum
    {
        Running = 0,
        Done = 1,
        Suspend = 2,
        PayOut = 3
    }
    
    public enum LotteryPrizeCategory
    {
        FirstPrize = 1,
        SecondPrize = 2,
        ThirdPrize = 3,
        ConsolationPrize = 4,
        SmallPrize = 5
    }
}