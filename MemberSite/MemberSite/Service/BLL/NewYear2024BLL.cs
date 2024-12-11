using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL;

public class NewYear2024BLL
{
    private readonly IEventsService _eventsService;
    private readonly SelLangBLL _selLangBll;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly EventsBLL _eventsBll;
    private readonly ILogger<NewYear2024BLL> _logger;
    private readonly ICacheHelper _redisHelper;

    public NewYear2024BLL(IServiceProvider provider)
    {
        _eventsService = provider.GetRequiredService<IEventsService>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _eventsBll = provider.GetRequiredService<EventsBLL>();
        _logger = provider.GetRequiredService<ILogger<NewYear2024BLL>>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
    }

    /// <summary>
    /// 檢查每日任務進度
    /// </summary>
    public async Task<IEnumerable<NewYearMission2024>> CheckCustPromotionMission(Customer customer, DateTime starDate = new DateTime())
    {
        var result = new List<NewYearMission2024>();
        DateTimeOffset startTimeTimeZone;
        var landId = _selLangBll.GetLangId();
        if (starDate != new DateTime())
        {
            startTimeTimeZone = await _dateTimeHelper.ToCurrencyZone(starDate, customer.CurrencyId);
        }
        else
        {
            startTimeTimeZone = await _dateTimeHelper.ToCurrencyZone(DateTime.UtcNow, customer.CurrencyId);
        }

        var startTime = startTimeTimeZone.DateTime.Date;
        var endTime = startTime.AddDays(1).AddSeconds(-1);

        var missionDate = startTime.ToString("yyyyMMdd");

        var promotionMission = _eventsService.GetPromotionMission(customer.SiteId, customer.CurrencyId, missionDate).ToList();

        if (!promotionMission.Any())
        {
            return result;
        }

        var tpCategoryId = promotionMission.Any(x => x.TpCategoryId != 0) ? promotionMission.FirstOrDefault(x => x.TpCategoryId != 0).TpCategoryId : 0;
        var progress = _eventsService.GetCustPromotionMissionProgress(customer.CustId, customer.CurrencyId, tpCategoryId, startTime, endTime);
        var messageList = _eventsService.GetPromotionMissionMessage();

        foreach (var mission in promotionMission)
        {
            PromotionMissionTypeEnum type;
            decimal goalAmount;
            decimal progressAmount;
            string message;

            if (mission.DepositFlag)
            {
                goalAmount = mission.DepositAmount;
                progressAmount = progress.UnUsedDepositAmount;
                type = PromotionMissionTypeEnum.AccumulateDeposit;
                mission.IsCompleted = progress.UnUsedDepositAmount >= mission.DepositAmount;
                message = messageList.FirstOrDefault(x => x.Type == (int)type && x.LangId == landId && x.TpCategoryId == 0)?.Message;
            }
            else if (mission.TurnoverFlag)
            {
                goalAmount = mission.TurnoverAmount;
                progressAmount = progress.TurnoverSum;
                type = PromotionMissionTypeEnum.AccumulateValidTurnover;
                mission.IsCompleted = progress.TurnoverSum >= mission.TurnoverAmount;
                message = messageList.FirstOrDefault(x => x.Type == (int)type && x.LangId == landId && x.TpCategoryId == tpCategoryId)?.Message;
            }
            else
            {
                goalAmount = mission.AffCount;
                progressAmount = progress.AffCount;
                type = PromotionMissionTypeEnum.SuccessfullyReferAFriend;
                mission.IsCompleted = progress.AffCount >= mission.AffCount;
                message = messageList.FirstOrDefault(x => x.Type == (int)type && x.LangId == landId && x.TpCategoryId == 0)?.Message;
            }

            var missionName = type switch
            {
                PromotionMissionTypeEnum.AccumulateDeposit when landId == "en" => string.Format(message, Enum.GetName(typeof(CurrencyId), customer.CurrencyId), (int)goalAmount),
                PromotionMissionTypeEnum.SuccessfullyReferAFriend => message,
                _ => string.Format(message, (int)goalAmount)
            };

            var model = new NewYearMission2024
            {
                MissionDate = mission.MissionName,
                MissionName = missionName,
                MissionNo = mission.MissionNo,
                GoalAmount = goalAmount,
                ProgressAmount = progressAmount,
                EggCount = mission.TicketCount,
                IsCompleted = mission.IsCompleted,
                Type = type,
                TpCategoryId = mission.TpCategoryId
            };

            result.Add(model);
        }

        return result;
    }

    /// <summary>
    /// 檢查目前龍蛋數量
    /// </summary>
    public async Task<int> GetEggTicket(int siteId,int custId,int currencyId)
    {
        var eggTicket = _eventsService.GetEggTicket(siteId,custId,currencyId);
        return eggTicket;
    }
    
    /// <summary>
    /// 索取龍蛋資料
    /// </summary>
    public async Task<IEnumerable<EggTicketData>> GetEggTicketData(int custId)
    {
        var eggTicket = _eventsService.GetEggTicketData(custId);
        return eggTicket;
    }

    /// <summary>
    /// 領取達成每日任務
    /// </summary>
    public int ClaimPromotionMission(Customer customer, IEnumerable<NewYearMission2024> mission, int eggTicket)
    {
        var missionDate = mission.FirstOrDefault().MissionDate;
        var missionNos = mission.Select(m => m.MissionNo);

        // 檢查已領取過任務
        var custPromotionMission = _eventsService.GetCustPromotionMission(customer.CustId, missionDate).ToList();
        var claimedMissionNos = custPromotionMission.Select(m => m.MissionNo);

        // 排除掉已領取的任務
        var insertMissionNos = missionNos.Except(claimedMissionNos).ToList();
        if (insertMissionNos.Any())
        {
            var insertMissions = mission.Where(m => insertMissionNos.Contains(m.MissionNo));
            var afterEggTicket = _eventsService.ClaimPromotionMission(customer.CustId, customer.SiteId, customer.CurrencyId, missionDate, insertMissions);

            _logger.LogInformation($"CustId :{customer.CustId} ,Claim PromotionMission ={JsonConvert.SerializeObject(insertMissions)}, Eggs = {insertMissions.Sum(x => x.EggCount)} ,Total Eggs : {afterEggTicket}");
            return afterEggTicket;
        }

        return eggTicket;
    }

    /// <summary>
    /// 抽獎開抽
    /// </summary>
    public async Task<(int error, List<Prize> prize)> DrawPrize(int eventId, Customer customer, int drawTimes)
    {
        var prizes = _eventsService.GetPrizes(eventId, customer.CurrencyId).Result.ToList();
        if (!prizes.Any()) return (105, null);

        if (string.IsNullOrEmpty(customer.AccountName) || !customer.PhoneVerified) return (104, null);

        //檢查有無票,是否足夠
        var tickets = _eventsService.GetEggTicket(customer.SiteId,customer.CustId,customer.CurrencyId);
        if (tickets == 0) return (102, null);
        if (tickets < drawTimes) return (106, null);

        var drawPrizeList = new List<Prize>();

        var remainingDraw = drawTimes;
        try
        {
            var stickers = _eventsService.GetCustBlessingsSticker(customer.CustId);
            ChangePrizeRate(prizes, stickers);

            // 開抽
            for (var i = 0; i < drawTimes; i++)
            {
                // -99 : 抽滿了 重抽
                int payoutPrizeOutput;
                do
                {
                    var drawPrize = Drawing(prizes);

                    if (string.Equals(drawPrize.PrizeName, "Blessings Sticker", StringComparison.InvariantCulture))
                    {
                        stickers += 1;
                        if (stickers >= 5)
                        {
                            ChangePrizeRate(prizes, stickers);
                        }
                    }

                    payoutPrizeOutput = await _eventsService.PayoutPrize(
                        eventId,
                        drawPrize.PrizeId,
                        drawPrize.PrizeName,
                        customer.SiteId,
                        customer.CustId,
                        customer.UserName,
                        customer.CurrencyId,
                        drawPrize.Amount,
                        0,
                        drawPrize.PrizeType,
                        true,
                        false
                    );

                    if (payoutPrizeOutput > 0)
                    {
                        drawPrizeList.Add(drawPrize);
                        remainingDraw -= 1;
                    }

                } while (payoutPrizeOutput == -99);

            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"CustId:{customer.CustId} ,DrawTimes :{drawTimes} ,Remaining Draw :{remainingDraw} , DrawPrize Error");
            return ((int)ErrorCode.Success, drawPrizeList);
        }

        return ((int)ErrorCode.Success, drawPrizeList);
    }

    /// <summary>
    /// 取得蒐集福字貼紙數量
    /// </summary>
    public async Task<int> GetCustBlessingsSticker(int custId)
    {
        var sticker = _eventsService.GetCustBlessingsSticker(custId);
        return sticker;
    }

    /// <summary>
    /// 檢查大獎領取狀態
    /// </summary>
    public bool CheckNewYearBigPrize(Customer customer, int eventId)
    {
        var claimedPrizeList = _eventsBll.GetCustDrawPrizeList(eventId, customer.SiteId, customer.CustId);
        if (claimedPrizeList.Any(x => x.PrizeName.StartsWith("Five Fortunes")))
            return true;

        return false;
    }

    /// <summary>
    /// 領取大獎
    /// </summary>
    public async Task<(int, Prize)> ClaimNewYearBigPrize(Customer customer, int eventId)
    {
        if (CheckNewYearBigPrize(customer, eventId))
            return (-2, null);

        var sticker = _eventsService.GetCustBlessingsSticker(customer.CustId);
        if (sticker >= 5)
        {
            var prizes = await _eventsService.GetPrizes(eventId, customer.CurrencyId);

            var drawPrize = prizes.FirstOrDefault(x => x.PrizeName.StartsWith("Five Fortunes"));

            var res = await _eventsService.PayoutPrize(
                eventId,
                drawPrize.PrizeId,
                drawPrize.PrizeName,
                customer.SiteId,
                customer.CustId,
                customer.UserName,
                customer.CurrencyId,
                drawPrize.Amount,
                0,
                drawPrize.PrizeType,
                true,
                true
                );

            return (res, drawPrize);
        }
        return (-1, null);
    }

    public bool CheckKey()
    {
        var result = false;
        var key = $"ReCheckPromotionMission";
        var value = _redisHelper.StringGet(key);
        if (value == "1")
        {
            result = true;
        }

        return result;
    }

    #region Private

    /// <summary>
    /// 抽獎機率邏輯
    /// </summary>
    private Prize Drawing(IEnumerable<Prize> prizes)
    {
        var drawPrize = new Prize();
        var rate = prizes.Sum(a => a.Rate);
        if (rate > 0)
        {
            var r = new Random();
            var luckyNum = r.Next(rate) + 1;
            var luckyCnt = 0;

            foreach (var prize in prizes.Where(x => x.Rate > 0))
            {
                drawPrize = prize;
                luckyCnt += prize.Rate;
                if (luckyNum <= luckyCnt)
                {
                    break;
                }
            }
        }

        return drawPrize;
    }

    private void ChangePrizeRate(List<Prize> prizes, int stickers)
    {
        // move sticker Rate to 銘謝惠顧 5%
        if (stickers >= 5)
        {
            var stickerPrize = prizes.FirstOrDefault(x => string.Equals(x.PrizeName, "Blessings Sticker", StringComparison.InvariantCulture));
            if (stickerPrize != null)
            {
                var changeRate = stickerPrize.Rate;
                stickerPrize.Rate = 0;

                var noPrize = prizes.FirstOrDefault(x => x.PrizeType == 1 && x.Amount == 0);
                if (noPrize != null) noPrize.Rate += changeRate;
            }
        }
    }

    #endregion

}