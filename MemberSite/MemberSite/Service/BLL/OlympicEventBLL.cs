using System.Reflection;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL;

public class OlympicEventBLL : IOlympicEventBLL
{
    private readonly CustInfoBll _custInfoBll;
    private readonly CupEventService _cupEventService;
    private readonly CupEventBLL _cupEventBLL;
    private readonly IEventsService _eventsService;
    private readonly MySqlClient _mainDbClient;
    private readonly PartitionService _partitionService;
    private readonly NewYear2024BLL _newYear2024Bll;
    private readonly ICacheHelper _redisHelper;
        
    public OlympicEventBLL(IServiceProvider provider)
    {
        _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        _partitionService = provider.GetRequiredService<PartitionService>();
        _cupEventBLL = provider.GetRequiredService<CupEventBLL>();
        _cupEventService = provider.GetRequiredService<CupEventService>();
        _eventsService = provider.GetRequiredService<IEventsService>();
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        _newYear2024Bll = provider.GetRequiredService<NewYear2024BLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
    }
    
    public async Task<OlympicMemberDepositTicketInfoResponse> GetOlympicEventTicket(int siteId, int custId, string userName, string langId,
        int currencyId)
    {
        var responseData = new OlympicMemberDepositTicketInfoResponse();
        var depositNowGoldMetal = 0;
        var turnoverNowGoldMetal = 0;
            
        var custInfoProfile = _custInfoBll.GetCustInfoProfile(custId);
        // 檢查驗證
        // 早點驗證 免得算完才發現沒驗證過 浪費資源
        if (!CheckCustVerifyStatus(siteId, custInfoProfile))
        {
            responseData.ErrorCode = 104;
            return responseData;
        }
            
        var olympicEventName = CupEventNameEnum.ParisOlympic2024.ToString();
        var promotionMission = GetPromotionMission(siteId, currencyId, olympicEventName).ToList();
            
        // egg = GoldMedal(金牌)
        var eggTicket = await _newYear2024Bll.GetEggTicketData(custInfoProfile.CustId);
        // 這邊算出來的都是已經扣完領過的數量金牌
        var olympicEventPromotionMissionProgress = GetOlympicEventPromotionMissionProgress(siteId, custInfoProfile, promotionMission, eggTicket);
            
        // 未驗證前給的資料
        responseData.UserName = userName;
        responseData.PromotionMission = promotionMission;
        responseData.OlympicEventPromotionMissionProgress = olympicEventPromotionMissionProgress;
        
        #region 頒獎
        //存款金額或流水大於任務的條件就直接幫忙換 不分Site 只看Currency
        //先算存款
        var depositGoldMetal = CalculateDivision(olympicEventPromotionMissionProgress.DepositAmount, promotionMission.Max(x => x.DepositAmount));

        if (depositGoldMetal.Count != 0)
        { 
            depositNowGoldMetal = _eventsService.OlympicsInertGoldMetal(custId, depositGoldMetal.Count, "Olympic2024Deposit");
            responseData.OlympicEventPromotionMissionProgress.DepositAmount = depositGoldMetal.Remainder;
        }
        //在算流水
        var turnoverGoldMetal = CalculateDivision(olympicEventPromotionMissionProgress.TurnoverSum,
            promotionMission.Max(x => x.TurnoverAmount));

        if (turnoverGoldMetal.Count != 0)
        {
            turnoverNowGoldMetal = _eventsService.OlympicsInertGoldMetal(custId, turnoverGoldMetal.Count, "Olympic2024Turnover");
            responseData.OlympicEventPromotionMissionProgress.TurnoverSum = turnoverGoldMetal.Remainder;
        }

        var availableGoldMetal = await CalculateLastGoldMetal(custId, depositNowGoldMetal, turnoverNowGoldMetal,eggTicket);
        
        responseData.AvailableGoldMetals = availableGoldMetal;
            
        #endregion
            
        return responseData;
    }
    
    /// <summary>
    /// 取得任務活動，不過期
    /// </summary>
    /// <returns></returns>
    private IEnumerable<PromotionMission> GetPromotionMission(int siteId, int currencyId, string missionName)
    {
        var promotionMissionKey = $"promotionMissionKey:{siteId}:{currencyId}:{missionName}";
        var promotionMission = _redisHelper.TryGetOrCreate(promotionMissionKey, TimeSpan.FromDays(60),
            () => _eventsService.GetPromotionMission(siteId, currencyId, missionName).ToList());
            
        return promotionMission;
    }
    
    private bool CheckCustVerifyStatus(int siteId, CustInfoProfile custInfo)
    {
        var status = false;
        switch (siteId)
        {
            case 1 or 2:
                if (custInfo.PhoneVerified && custInfo.EmailVerified) status = true;
                break;
            case 3:
                status = true;
                break;
        }
        return status;
    }
    
    private OlympicEventPromotionMissionProgress GetOlympicEventPromotionMissionProgress(int siteId, CustInfoProfile custInfo,
        IReadOnlyCollection<PromotionMission> promotionMission , IEnumerable<EggTicketData> eggTicket)
    {
        var response = new OlympicEventPromotionMissionProgress();
        
        // Deposit GoldMedal
        var depositGoldMedal = eggTicket.Count(x => x.MissionDate.Contains("Olympic2024Deposit"));
        
        // Turnover GoldMedal
        var turnOverGoldMedal = eggTicket.Count(x => x.MissionDate.Contains("Olympic2024Turnover"));
        
        // Calculate Deposit
        var depositList = _cupEventService.GetMemberDeposit(siteId ,custInfo.CustId, custInfo.CurrencyId).ToList();
        var promotionMissionDepositAmount = promotionMission.Max(x => x.DepositAmount);
        var totalDepositAmount = OlympicEventDepositAmountCalculate(depositList, promotionMissionDepositAmount, depositGoldMedal);
        response.DepositAmount = totalDepositAmount;
        
        // Calculate Turnover
        var promotionMissionTurnoverSum = promotionMission.Max(x => x.TurnoverAmount);
        var totalTurnoverAmount = OlympicEventTurnoverCalculate(custInfo, promotionMissionTurnoverSum, turnOverGoldMedal);
        response.TurnoverSum = totalTurnoverAmount;
            
        return response;
    }

    private decimal OlympicEventDepositAmountCalculate(IEnumerable<MemberDepositDto> depositList,
        decimal promotionMissionDepositAmount, int depositGoldMedalCount)
    {
        decimal resultAmount;
        var depositListAmount = depositList.Sum(x => x.DepositAmount);
        if (depositListAmount != 0)
        {
            resultAmount = depositListAmount - (depositGoldMedalCount * promotionMissionDepositAmount);
        }
        else
        {
            //這邊要改成0 要不然會一直加金牌 虧爛
            resultAmount = 0;
        }
        
        return resultAmount;
    }
    
    private decimal OlympicEventTurnoverCalculate(CustInfoProfile custInfo,decimal promotionMissionTurnover, int turnoverGoldMedalCount)
    {
        //直接取已結算且符合規則的流水單
        //20240606 : 2024此次奧運規則為Olympic相關賽事都算 需結算單
        var SpName = "Service_Olympic_TpBetRecord_Get";
        var tpId = Tp.SBSW;
        var gameType = "69";
        var sq = 1111;
        //Defaul Result
        var result = 0;
        
        //先寫死 後續看怎拿
        var startTime = DateTime.Parse("2024-05-01 00:00:00");
        var endTime = DateTime.Parse("2024-07-01 00:00:00");

        var p = new
        {
            CustId = custInfo.CustId,
            TpId = tpId,
            GameType = gameType,
            StartTime = startTime,
            EndTime = endTime
        };

        var getTpBetRecordData = _partitionService.GetTpBetRecord(startTime, endTime, SpName, p);
        if (getTpBetRecordData.Any())
        {
            var joinedTransId = GetTransIdSqlInClause(getTpBetRecordData);
            var getSBSWBetData = GetSBSWSportBet(sq, custInfo.CustId, joinedTransId);
            if (getSBSWBetData.Any())
            {
                var totalValidBet = CalculateValidBet(getTpBetRecordData, getSBSWBetData);
                return totalValidBet - (turnoverGoldMedalCount * promotionMissionTurnover);
            }
        }
        
        return result; 
    }
    
    private string GetTransIdSqlInClause(IEnumerable<TpBetRecord> tpBetRecords)
    {
        var transIdValues = tpBetRecords
            .Where(record => !string.IsNullOrEmpty(record.TransId))
            .Select(record => record.TransId);

        return string.Join(",", transIdValues);
    }


    private IEnumerable<string> GetSBSWSportBet(int sq,int custId ,string transIdList)
    {
        var result = new List<string>();
        var getSportBet = _eventsService.GetOlympicsEventSportBet(sq, custId, transIdList);
        if (getSportBet.Any())
        {
            return getSportBet
                .Where(bet => bet.leagueName_en.Contains("Olympics Paris 2024"))
                .Select(bet => bet.txId);
        }
        return result;
    }

    private decimal CalculateValidBet(IEnumerable<TpBetRecord> getTpBetRecordData, IEnumerable<string> getSBSWBetData)
    {
        var sbswBetDataSet = new HashSet<string>(getSBSWBetData);
        var totalValidBet = getTpBetRecordData
            .Where(record => sbswBetDataSet.Contains(record.TransId))
            //先用Stake 因為都沒辦法結算
            //.Sum(record => record.ValidBet ?? 0);
            .Sum(record => record.Stake);
        
        return totalValidBet;
    }
    
    private OlympicEventDivisionResult CalculateDivision(decimal total, decimal divisor)
    {
        if (divisor == 0)
        {
            throw new ArgumentException("Divisor cannot be zero.");
        }

        var count = (int)(total / divisor);
        var remainder = total - (count * divisor);

        return new OlympicEventDivisionResult
        {
            Count = count,
            Remainder = remainder
        };
    }

    private async Task<int> CalculateLastGoldMetal(int custId,int depositGoldMetal, int turnoverGoldMetal,IEnumerable<EggTicketData> eggTickets)
    {
        if (depositGoldMetal == 0 && turnoverGoldMetal == 0)
        {
            // var eggTicket = await _newYear2024Bll.GetEggTicketData(custId);
           
            // Deposit GoldMedal
            var depositNowGoldMedal = eggTickets
                .Count(x => x.MissionDate.Contains("Olympic2024Deposit") && x.IsUsed == 0);
        
            // Turnover GoldMedal
            var turnOverNowGoldMedal = eggTickets
                .Count(x => x.MissionDate.Contains("Olympic2024Turnover") && x.IsUsed == 0);
            
            return depositNowGoldMedal + turnOverNowGoldMedal;
        }
        
        return depositGoldMetal + turnoverGoldMetal;
    }
}