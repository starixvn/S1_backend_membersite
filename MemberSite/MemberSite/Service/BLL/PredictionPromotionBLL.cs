using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL;

public class PredictionPromotionBLL
{
    private readonly IEventsService _eventsService;
    private readonly SelLangBLL _selLangBll;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly EventsBLL _eventsBll;
    private readonly ILogger<NewYear2024BLL> _logger;
    private readonly ICacheHelper _redisHelper;

    public PredictionPromotionBLL(IServiceProvider provider)
    {
        _eventsService = provider.GetRequiredService<IEventsService>();
        _selLangBll = provider.GetRequiredService<SelLangBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _eventsBll = provider.GetRequiredService<EventsBLL>();
        _logger = provider.GetRequiredService<ILogger<NewYear2024BLL>>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
    }
    
    public async Task<IEnumerable<GetPredictionTeamInfoResp>> GetPredictionTeamInfo(int custId ,int siteId, int currency ,string landId)
    {
        var promotionPredictionList = await _eventsService.GetPromotionPrediction(custId, siteId, currency, landId);
        var result = promotionPredictionList
            .GroupBy(x => x.BonusCode)
            .Select(r => new GetPredictionTeamInfoResp
            {
                BonusCode = r.Key,
                PromotionPrediction = promotionPredictionList.Where(x=>x.BonusCode == r.Key).ToList()
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