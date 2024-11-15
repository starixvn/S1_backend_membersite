using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL;

public class PredictionPromotionBLL
{
    private readonly IEventsService _eventsService;
    private readonly HotEventBLL _hotEventBll;
    private readonly EventsBLL _eventsBll;
    
    public PredictionPromotionBLL(IServiceProvider provider)
    {
        _eventsService = provider.GetRequiredService<IEventsService>();
        _hotEventBll = provider.GetRequiredService<HotEventBLL>();
        _eventsBll = provider.GetRequiredService<EventsBLL>();
    }
    
    public async Task<IEnumerable<GetPredictionTeamInfoResp>> GetPredictionTeamInfo(int custId ,ServerInfo serverInfo, int currency ,string landId)
    {
        var promotionPredictionList = await _eventsService.GetPromotionPrediction(custId, serverInfo.SiteId, currency, landId);
        var sportTeamsInfo = await _hotEventBll.GetSportTeamsInfo();
        var sportLeaguesInfo = await _hotEventBll.GetSportLeaguesInfo();
        
        var result = promotionPredictionList
            .GroupBy(x => x.BonusCode)
            .Select(r => new GetPredictionTeamInfoResp
            {
                BonusCode = r.Key,
                PromotionPrediction = promotionPredictionList
                    .Where(x=>x.BonusCode == r.Key)
                    .Select(y => new PromotionPrediction
                    {
                        BonusCode = r.Key,
                        MatchId = y.MatchId,
                        Banner = y.Banner,
                        Description = y.Description,
                        RollOver = y.RollOver,
                        RequiredAmount = y.RequiredAmount,
                        Bonus = y.Bonus,
                        SportLeaguesTitle = y.SportLeaguesTitle,
                        GameId =  y.GameId,
                        BattleTime =  y.BattleTime,
                        HomeTeamName =  y.HomeTeamName,
                        HomeTeamLeaguesId = y.HomeTeamLeaguesId,
                        HomeTeamId = y.HomeTeamId,
                        HomeTeamPic =  _hotEventBll.GetHomeTeamPic(serverInfo, sportLeaguesInfo, y, sportTeamsInfo),
                        AwayTeamLeaguesId =y.AwayTeamLeaguesId,
                        AwayTeamId = y.AwayTeamId,
                        AwayTeamName =  y.AwayTeamName,
                        AwayTeamPic = _hotEventBll.GetAwayTeamPic(serverInfo, sportLeaguesInfo, y, sportTeamsInfo),
                        HomeScore =  y.HomeScore,
                        AwayScore =  y.AwayScore,
                        Result =  y.Result,
                        IsPredict =  y.IsPredict,
                        TpId = y.TpId,
                    })
            });
        
        return result;
    }
    
    public async Task<IEnumerable<PredictScoreModel>> GetPredictScoreList(Customer cust ,string landId)
    {
        var result = await _eventsService.GetPredictScoreList(cust.CustId, cust.SiteId, cust.CurrencyId, landId);
        
        return result;
    }
    
    public async Task<PredictionWinnerListResponse> GetPredictionResultTeamInfo(int siteId, int currencyId ,string langId)
    {
        var promotionPrediction = await _eventsService.GetPredictionResultTeamInfo(siteId, currencyId, langId);
        var winnerList = await _eventsService.GetPromotionPredictionWinnerList(siteId, promotionPrediction.MatchId);
        
        var response = new PredictionWinnerListResponse
        {
            SportLeaguesTitle = promotionPrediction.SportLeaguesTitle,
            MatchId = promotionPrediction.MatchId,
            HomeTeamLeaguesId = promotionPrediction.HomeTeamLeaguesId,
            HomeTeamId = promotionPrediction.HomeTeamId,
            HomeTeamName = promotionPrediction.HomeTeamName,
            AwayTeamLeaguesId = promotionPrediction.AwayTeamLeaguesId,
            AwayTeamId = promotionPrediction.AwayTeamId,
            AwayTeamName = promotionPrediction.AwayTeamName,
            BattleTime = promotionPrediction.BattleTime,
            Bonus = promotionPrediction.Bonus,
            IsoCode = promotionPrediction.IsoCode,
            Winners = winnerList.Any() ? winnerList.Select(x=>_eventsBll.MaskName(x.UserName)).ToList() : new List<string>()
        };
        
        return response;
    }
    
    public async Task<PromotionPrediction> GetPromotionPredictionByMatchId(int siteId, int matchId)
    {
        var promotionPrediction = await _eventsService.GetPromotionPredictionByMatchId(siteId, matchId);
      
        return promotionPrediction;
    }
    
    public async Task<bool> PredictCupEventScore( int custId,int matchId, PredictScoreRequest request)
    {
        if (request.AwayTeamScore < 0 || request.HomeTeamScore < 0)
        {
            return false;
        }
            
        var result = await _eventsService.PromotionPredictScore(custId, matchId ,request);
        return result > 0;
    }

    public async Task<List<PredictionDepositDto>> GetMemberDeposit(int custId, DateTime startTime, DateTime endTime)
    {
        var promotionPrediction = await _eventsService.GetMemberDeposit(custId, startTime ,endTime);
      
        return promotionPrediction;
    }
}