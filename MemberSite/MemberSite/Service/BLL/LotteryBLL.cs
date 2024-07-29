using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class LotteryBLL
    {
        private readonly EventsService _eventsService;
        private readonly CacheHelper _cacheHelper;
        
        public LotteryBLL(IServiceProvider provider)
        {
            _eventsService = provider.GetRequiredService<EventsService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }
        
        public ( LotteryInfo, int) GetLotteryInfo(int siteId , int custId , int currencyId)
        {
            int errorCode;
            var lotteryInfo = new LotteryInfo
            {
                LotteryRoundInfo = null,
                LotteryTierList = null,
                TicketNo = null,
                CustTurnoverProccessing = 0,
                CustTurnoverSummary = 0
            };
            
            var lotteryRoundInfo = GetLotteryRoundInfo(siteId, DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).DateTime);
            if (lotteryRoundInfo is null)
            {
                errorCode = -1;
                return ( lotteryInfo , errorCode);
            }
            
            var ticketStickerSetting = _eventsService.GetRaffleTicketStickerSetting(siteId).ToList();
            var setting = ticketStickerSetting.FirstOrDefault(x => x.CurrencyId == currencyId);
            if (setting is null)
            {
                errorCode = -2;
                return ( lotteryInfo , errorCode);
            }
            
            var tierCurrencyList = GetLotteryTierList(siteId).Where(x=>x.CurrencyId == currencyId).ToList();
            if (!tierCurrencyList.Any())
            {
                errorCode = -3;
                return ( lotteryInfo , errorCode);
            }
            
            var originTicketList = _eventsService.GetCustLotteryTicket(custId, lotteryRoundInfo.RoundId ,(int)LotteryTicketEnum.All).ToList();
            var custTicketList = originTicketList.Where(x=>x.Type == (int)LotteryTicketEnum.Ticket).Select(x => x.TicketNo).ToList();
            
            var turnoverSummary = _eventsService.GetCustLotteryTurnOverSummary(custId, lotteryRoundInfo.StartDate.UtcDateTime, lotteryRoundInfo.EndDate.UtcDateTime);
            var proccessing = turnoverSummary - setting.TurnOver * custTicketList.Count;
            
            lotteryInfo.LotteryRoundInfo = lotteryRoundInfo;
            lotteryInfo.LotteryTierList = tierCurrencyList;
            lotteryInfo.TicketNo = custTicketList;
            lotteryInfo.CustTurnoverSummary = turnoverSummary;
            lotteryInfo.CustTurnoverProccessing = proccessing;
            lotteryInfo.TurnoverGoal = setting.TurnOver;
            errorCode = 0;
            
            return (lotteryInfo , errorCode);
        }
        
        public GetCurrentLotteryWinnerListResponse GetCurrentLotteryWinnerList(int siteId , int roundId)
        {
            var response = new GetCurrentLotteryWinnerListResponse();
            var result = GetLotteryWinnerListCache(siteId , roundId)
                .MaxBy(x=>x.CreateTime);

            if (result is not null)
            {
                response.SiteId = result.SiteId;
                response.RoundId = result.RoundId;
                response.FirstPrize = JsonConvert.DeserializeObject<List<string>>(result.FirstPrize);
                response.SecondPrize = JsonConvert.DeserializeObject<List<string>>(result.SecondPrize);
                response.ThirdPrize = JsonConvert.DeserializeObject<List<string>>(result.ThirdPrize);
                response.ConsolationPrize = JsonConvert.DeserializeObject<List<string>>(result.ConsolationPrize);
                response.SmallPrize = JsonConvert.DeserializeObject<List<string>>(result.SmallPrize);
                response.StickerId = result.StickerId;
            }
            else
            {
                return null;
            }
            
            return response;
        }
        
        public List<GetLotteryRoundListResponse> GetLotteryRoundList(int siteId , int roundId)
        {
            var result = GetLotteryWinnerListCache(siteId , roundId);

            var response = result.Select(x => new GetLotteryRoundListResponse 
                {
                    SiteId = x.SiteId,
                    RoundId = x.RoundId,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate
                })
                .OrderByDescending(x=>x.RoundId)
                .ToList();

            return response;
        }
        
        public LotteryEventDraw GetLotteryRoundInfo(int siteId, DateTime date)
        {
            var key = $"GetLotteryRoundInfo_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                _ => _eventsService.GetLotteryEventDrawSetting(siteId , date));
        }
        
        public IEnumerable<LotteryEventDrawTurnOverSetting> GetLotteryTierList(int siteId)
        {
            var key = $"GetLotteryTierList_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                _ => _eventsService.GetLotteryEventDrawTurnOverSetting(siteId));
        }
        
        private IEnumerable<CurrentLotteryWinnerList> GetLotteryWinnerListCache(int siteId ,int roundId)
        {
            var key = $"GetCurrentLotteryWinnerList_{siteId}_{roundId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)),
                _ => _eventsService.GetCurrentLotteryWinnerList(siteId , roundId));
        }
        
        public IEnumerable<RaffleTicketStickerSetting> GetRaffleTicketStickerSetting(int siteId)
        {
            var list = _eventsService.GetRaffleTicketStickerSetting(siteId);
            return list;
        }
        
        public IEnumerable<CustLotteryTicket> GetCustLotteryTicket(int custId, int roundId, int type)
        {
            var list = _eventsService.GetCustLotteryTicket(custId, roundId, type);
            return list;
        }

        public async Task<int> BuyChineseZodiacSticker(Customer custInfo,int roundId,int stickerId,decimal stickerPrize)
        {
            var custLotteryTicketRequest = new CustLotteryTicketInsertRequest
            {
                CustId = custInfo.CustId,
                TicketNo = "",
                RoundId = roundId,
                StickerId = stickerId,
                Type = 2
            };
            
            var insertResult = _eventsService.InsertCustLotteryTicket(custLotteryTicketRequest);

            if (insertResult != 1)
            {
                return 997;
            }
            
            var balanceMemo = $"Buy ChineseZodiacSticker - StickerId : {stickerId}, Prize : {stickerPrize:#,##0.##}.";

            var buyStickerRequest = new BuyZodiacStickerRequest
            {
                SiteId = custInfo.SiteId,
                CustId = custInfo.CustId,
                UserName = custInfo.UserName,
                StickerPrize = stickerPrize * -1,
                Memo = balanceMemo
            };
            

            var buyResult = await _eventsService.BuyChineseZodiacSticker(buyStickerRequest);

            if (buyResult != 1)
            {
                return 998;
            }
            
            return buyResult;
        }
        
    }
}

