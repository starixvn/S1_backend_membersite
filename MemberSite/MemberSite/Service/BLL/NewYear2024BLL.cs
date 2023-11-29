using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL;

public class NewYear2024BLL 
{
    private readonly IEventsService _eventsService;
    private readonly SelLangBLL _selLangBll;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly ILogger<NewYear2024BLL> _logger;

    public NewYear2024BLL(IServiceProvider provider)
    {
        _eventsService = provider.GetRequiredService<IEventsService>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _logger = provider.GetRequiredService<ILogger<NewYear2024BLL>>();
    }

    public async Task<IEnumerable<NewYearMission2024>> CheckPromotionMission(Customer customer)
    {
        var result = new List<NewYearMission2024>();
            
        var landId = _selLangBll.GetLangId();
        var startTimeTimeZone = await _dateTimeHelper.ToCurrencyZone(DateTime.UtcNow, customer.CurrencyId);
        var startTime = startTimeTimeZone.DateTime.Date;
        var endTime = startTime.AddDays(1).AddSeconds(-1);
        
        var missionDate = startTime.ToString("yyyyMMdd");
            
        var promotionMission = _eventsService.GetPromotionMission(customer.SiteId, customer.CurrencyId, missionDate).ToList();

        if (!promotionMission.Any())
        {
            return null;
        }
        
        var tpCategoryId = promotionMission.Any(x => x.TpCategoryId != 0) ? promotionMission.FirstOrDefault(x => x.TpCategoryId != 0).TpCategoryId : 0;
        var progress = _eventsService.GetPromotionMissionProgress(customer.CustId, customer.CurrencyId, tpCategoryId, startTime, endTime);
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
                message = messageList.FirstOrDefault(x => x.Type == (int) type && x.LangId == landId && x.TpCategoryId == 0)?.Message;
            }
            else if (mission.TurnoverFlag)
            {
                goalAmount = mission.TurnoverAmount;
                progressAmount = progress.TurnoverSum;
                type = PromotionMissionTypeEnum.AccumulateValidTurnover;
                mission.IsCompleted = progress.TurnoverSum >= mission.TurnoverAmount;
                message = messageList.FirstOrDefault(x => x.Type == (int) type && x.LangId == landId && x.TpCategoryId == tpCategoryId)?.Message;
            }
            else
            {
                goalAmount = mission.AffCount;
                progressAmount = progress.AffCount;
                type = PromotionMissionTypeEnum.SuccessfullyReferAFriend;
                mission.IsCompleted = progress.AffCount >= mission.AffCount;
                message = messageList.FirstOrDefault(x => x.Type == (int) type && x.LangId == landId && x.TpCategoryId == 0)?.Message;
            }
            
            var missionName = type switch
            {
                PromotionMissionTypeEnum.AccumulateDeposit when landId == "en" => string.Format(message, Enum.GetName(typeof(CurrencyId), customer.CurrencyId), (int)goalAmount ),
                PromotionMissionTypeEnum.SuccessfullyReferAFriend => message, 
                _ => string.Format(message, goalAmount)
            };

            var model = new NewYearMission2024
            {
                MissionDate = mission.MissionDate,
                MissionName = missionName,
                MissionNo = mission.MissionNo,
                GoalAmount = goalAmount,
                ProgressAmount = progressAmount,
                EggCount = mission.EggCount,
                IsCompleted = mission.IsCompleted
            };
            
            result.Add(model);
        }

        return result;
    }

    public async Task<int> GetEggTicket(int custId)
    {
        var eggTicket = _eventsService.GetEggTicket(custId);
        return eggTicket;
    }

    public int ClaimPromotionMission(Customer customer, IEnumerable<NewYearMission2024> mission , int eggTicket)
    {
        var missionDate = mission.FirstOrDefault().MissionDate;
        var missionNos = mission.Select(m => m.MissionNo).ToList();

        // 檢查已領取過任務
        var custPromotionMission = _eventsService.GetCustPromotionMission(customer.CustId, missionDate).ToList();
        var claimedMissionNos = custPromotionMission.Select(m => m.MissionNo);
        
        // 排除掉已領取的任務
        var insertMissionNos = missionNos.Except(claimedMissionNos).ToList();
        if (insertMissionNos.Any())
        {
            var insertMissions = mission.Where(m => insertMissionNos.Contains(m.MissionNo));
            var afterEggTicket = _eventsService.ClaimPromotionMission(customer.CustId, customer.SiteId, customer.CurrencyId, missionDate, insertMissions);
            
            _logger.LogInformation($"CustId :{customer.CustId} ,Claim PromotionMission ={JsonConvert.SerializeObject(insertMissions)}, Eggs = {insertMissions.Sum(x=>x.EggCount)} ,Total Eggs : {afterEggTicket}"); 
            return afterEggTicket; 
        }

        return eggTicket;
    }
    
}