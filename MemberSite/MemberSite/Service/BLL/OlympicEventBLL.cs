using System.Text.RegularExpressions;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MemberSite.Service.BLL;

public class OlympicEventBLL : IOlympicEventBLL
{
    private readonly CustInfoBll _custInfoBll;
    private readonly CupEventService _cupEventService;
    private readonly IEventsService _eventsService;
    private readonly CourierAdapter _courierAdapter;
    private readonly string _bonusApiUrl;
    private readonly PartitionService _partitionService;
    private readonly NewYear2024BLL _newYear2024Bll;
    private readonly Tools _tools;
    private readonly ICacheHelper _redisHelper;
    private readonly SelLangBLL _selLangBll;
    private readonly ILogger<OlympicEventBLL> _logger;
        
    public OlympicEventBLL(IServiceProvider provider)
    {
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        _partitionService = provider.GetRequiredService<PartitionService>();
        _cupEventService = provider.GetRequiredService<CupEventService>();
        _eventsService = provider.GetRequiredService<IEventsService>();
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        _newYear2024Bll = provider.GetRequiredService<NewYear2024BLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<OlympicEventBLL>>();
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _tools = provider.GetRequiredService<Tools>();
        _bonusApiUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").BonusService;
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

    public async Task<ClaimOlympicEventPrizeResponse> ClaimOlympicEventPrize(int siteId, int custId, string userName,int currencyId,int usedGoldMetalCount)
    {
        var result = new ClaimOlympicEventPrizeResponse();
        var bonusUrl = $"{_bonusApiUrl}SetAdjustBonus";
        // var bonusUrl = $"{_bonusApiUrl}SetClaimPromotion";
        int prizeId;
        string bonusCode;
        var olympicPrize = new Prize();
        var id = 10000;
        
        //獲取EventId及BonusCode
        var currencyBonusAmountList = GetOlympicsBonusAmount();
        if (!string.IsNullOrEmpty(currencyBonusAmountList))
        {
            var getCurrencyAmount = JObject.Parse(currencyBonusAmountList);
            bonusCode = (string)getCurrencyAmount["olympicsBonusCode"]?[usedGoldMetalCount.ToString()];
            prizeId = (int)getCurrencyAmount[currencyId.ToString()]?[usedGoldMetalCount.ToString()];
            
            var eventId = siteId switch
            {
                1 => 18,
                2 => 19,
                3 => 20,
                _ => 0
            };
            var prizesList = await _eventsService.GetPrizes(eventId, currencyId);
            olympicPrize = prizesList.FirstOrDefault(x => x.PrizeId == prizeId);
        }
        else
        {
            return new ClaimOlympicEventPrizeResponse()
            {
                ErrorCode = 999,
                Message = "Olympic 2024 - Can't Get Redis Setting"
            };
        }
        
        //金牌數不足不發 前端可能會擋 後端在擋一次
        var eggTicket = await _newYear2024Bll.GetEggTicketData(custId);
        var eggTicketCanUse = eggTicket.Where(x => x.IsUsed == 0 && x.MissionDate.Contains("Olympic2024")).ToList();
        if (eggTicketCanUse.Count < usedGoldMetalCount)
        {
            return new ClaimOlympicEventPrizeResponse()
            {
                ErrorCode = 789,
                Message = "Olympic 2024 - GoldMetal Insufficient"
            };
        }
        
        
        //先發紅利及寫Log 有問題至少不會動到金牌數
        #region Write Insert Log
        
        //原走正常領紅利流程，但CanJoin那邊會不過，先保留用後台調帳Bonus
        // var deviceType = (int)PromotionDeviceType.All;
        //
        // var bonusRequest = new BonusPayoutRequest
        // {
        //     CustId = custId,
        //     LockedAmount = olympicPrize.Amount,
        //     custIP = _tools.GetClientIP(),
        //     BonusCode = bonusCode,
        //     DeviceType = deviceType
        // };
        
        var bonusRequest = new BonusPayoutRequest
        {
            CustId = custId,
            Amount = olympicPrize.Amount,
            BonusCode = bonusCode,
            RollOver = 5,
            PromotionType = (int)PromotionType.SpecialBonus,
            Operator = "System",
            Memo = $"Olympics2024-BonusPayout",
            DepositId = ""
        };

        var detailInfo = new
        {
            UserName = userName,
            BonusCode = bonusCode,
            Memo = bonusRequest.Memo,
            Amount = bonusRequest.Amount,
            PromotionType = bonusRequest.PromotionType.ToString(),
            bonusRequest.DepositId
        };

        var logResult =
            _eventsService.CreateAdminAdjustmentInfoLog(new CreateAdminAdjustmentInfoLogInput
            {
                Operator = "Olympic2024System",
                SiteId = siteId,
                CurrencyId = currencyId,
                ActionType = ActionType.Create,
                MenuEnum = AdminAdjustmentLogMenuEnum.IssueBonusManagement,
                BeforeDetailInfo = null,
                AfterDetailInfo = JsonConvert.SerializeObject(detailInfo),
                RefTransId = string.Empty
            });

        if (!logResult)
        {
            result.ErrorCode = 456;
            result.Message = "Insert log error.";
            return result;
        }

        #endregion
        
        #region Payout Bonus
        
        var apiOnlineResult = await _courierAdapter.MyPostAsync<ApiResult, BonusPayoutRequest>(bonusUrl, bonusRequest);
        if (apiOnlineResult.ErrorCode == (int)ErrorCode.Success)
        {
            result.ErrorCode = (int)ErrorCode.Success;
            result.Message = $"Success.";
        }
        else
        {
            result.ErrorCode = apiOnlineResult.ErrorCode;
            result.Message = apiOnlineResult.Message;
            return result;
        }

        #endregion

        #region Set Used GoldMedal
        
        var getGoldMedalIdList = GetGoldMedalIdSqlInClause(eggTicket, usedGoldMetalCount);
        var setUsedGoldMedal = _eventsService.OlympicsEventGoldMetalSet(id, custId, getGoldMedalIdList);
        
        //先塞DrawLog 後續再看怎樣
        if (setUsedGoldMedal == (int)ErrorCode.Success)
        {
            var insertLog = _eventsService.OlympicsEventDrawLogIns
            (
                olympicPrize.EventId,
                olympicPrize.PrizeId,
                olympicPrize.PrizeName,
                olympicPrize.Amount,
                custId,
                currencyId
            );

            if (insertLog == 1)
            {
                return result;
            }

        }

        #endregion
        
        return result;
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

    private string GetOlympicsBonusAmount()
    {
        var olympicsBonusAmountKey = $"Olympic2024BonusAmount";
        var olympicsBonusAmount = _redisHelper.StringGet(olympicsBonusAmountKey);
        return olympicsBonusAmount;
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
    
    private string GetGoldMedalIdSqlInClause(IEnumerable<EggTicketData> eggTicketDatas,int usedGoldMetalCount)
    {
        var transIdValues = eggTicketDatas
            .Where(record => record.IsUsed == 0 && record.MissionDate.Contains("Olympic2024"))
            .OrderBy(record => record.CreateTime)
            .Take(usedGoldMetalCount)
            .Select(record => record.Id);

        return string.Join(",", transIdValues);
    }


    private IEnumerable<string> GetSBSWSportBet(int sq,int custId ,string transIdList)
    {
        var result = new List<string>();
        var getSportBet = _eventsService.GetOlympicsEventSportBet(sq, custId, transIdList);
        if (getSportBet.Any())
        {
            //查看注單也有別的關鍵詞 改用Regex匹配 要不然SABA那邊有改會錯
            result = getSportBet
                .Where(bet => Regex.IsMatch(bet.leagueName_en, @"\bOlympics\b.*\b2024\b", RegexOptions.IgnoreCase))
                .Select(bet => bet.txId).ToList();
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