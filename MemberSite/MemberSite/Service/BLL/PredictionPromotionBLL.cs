using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL;

public class PredictionPromotionBLL
{
    private readonly IEventsService _eventsService;
    private readonly HotEventBLL _hotEventBll;
    
    public PredictionPromotionBLL(IServiceProvider provider)
    {
        _eventsService = provider.GetRequiredService<IEventsService>();
        _hotEventBll = provider.GetRequiredService<HotEventBLL>();
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
                        Title = y.Title,
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

}