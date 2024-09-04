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
            var eventId = 21;
            int errorCode;
            var lotteryInfo = new LotteryInfo
            {
                LotteryRoundInfo = null,
                LotteryTierList = null,
                TicketNo = null,
                CustTurnoverProccessing = 0,
                CustTurnoverSummary = 0,
                JackpotAmount = 0
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
            
            var jackpotAccumulation = _eventsService.GetDrawPrizeJackpotAccumulation(eventId);
            
            lotteryInfo.LotteryRoundInfo = lotteryRoundInfo;
            lotteryInfo.LotteryTierList = tierCurrencyList;
            lotteryInfo.TicketNo = custTicketList;
            lotteryInfo.CustTurnoverSummary = turnoverSummary;
            lotteryInfo.CustTurnoverProccessing = proccessing;
            lotteryInfo.TurnoverGoal = setting.TurnOver;
            lotteryInfo.JackpotAmount = jackpotAccumulation.JackpotAmount;
            errorCode = 0;
            
            return (lotteryInfo , errorCode);
        }
        
        public GetCurrentLotteryWinnerListResponse GetCurrentLotteryWinnerList(int siteId ,int custId  , int roundId)
        {
            var response = new GetCurrentLotteryWinnerListResponse();
            var result = GetLotteryWinnerList(siteId , roundId)
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
                var tickets = _eventsService.GetCustLotteryTicket(custId, result.RoundId ,(int)LotteryTicketEnum.Ticket).ToList();
                response.TicketNo = tickets.Select(x => x.TicketNo).ToList();
                response.CustWinTicketNo = tickets.FirstOrDefault(x => x.LotteryPrizeCategory != 0);
                var sticker = _eventsService.GetCustLotteryTicket(custId, result.RoundId ,(int)LotteryTicketEnum.Sticker).FirstOrDefault();
                if (sticker != null) response.CustStickerId = sticker.StickerId;
            }
            else
            {
                return null;
            }
            
            return response;
        }
        
        public List<GetLotteryRoundListResponse> GetLotteryRoundList(int siteId)
        {
            var resultList = _eventsService.GetLotteryRoundList(siteId);
            
            // 抓drawdate + 1 month 以前全部資料
            var checkTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).AddMonths(1).DateTime;
            resultList = resultList.Where(x => x.DrawDate <= checkTime);
            
            var response = resultList.Select(x => new GetLotteryRoundListResponse 
                {
                    SiteId = x.SiteId,
                    RoundId = x.RoundId,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    DrawDate = x.DrawDate
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
        
        public List<GetCustLotteryWinningResponse> GetCustLotteryHistoryWinnerList(int siteId ,int custId, int roundId)
        {
            var response = new List<GetCustLotteryWinningResponse>();
            
            var winningTickets = GetCustLotteryTicket(custId, roundId, (int) LotteryTicketEnum.Ticket)
                .Where(x=>x.LotteryPrizeCategory != 0)
                .ToList();

            var sticker = GetCustLotteryTicket(custId, roundId, (int) LotteryTicketEnum.Sticker).FirstOrDefault();
            
            var roundList = GetLotteryWinnerList(siteId , roundId).ToList();

            foreach (var ticketData in winningTickets)
            {
                var roundMatch = roundList.FirstOrDefault(rl => rl.RoundId == ticketData.RoundId);
                if (roundMatch is not null)
                {
                    var data = new GetCustLotteryWinningResponse
                    {
                        RoundId = roundMatch.RoundId,
                        TicketNo = ticketData.TicketNo,
                        StartDate = roundMatch.StartDate,
                        EndDate = roundMatch.EndDate,
                        LotteryPrizeCategory =  ticketData.LotteryPrizeCategory,
                        StickerId = sticker?.StickerId ?? 0
                    };

                    if (ticketData.LotteryPrizeCategory == LotteryPrizeCategory.FirstPrize)
                    {
                        data.Jackpot = roundMatch.StickerId == data.StickerId ? 1 : 0;
                    }
                    
                    response.Add(data);
                }
            }
            
            return response;
        }
        
        public IEnumerable<LotteryEventDrawTurnOverSetting> GetLotteryTierList(int siteId)
        {
            var key = $"GetLotteryTierList_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                _ => _eventsService.GetLotteryEventDrawTurnOverSetting(siteId));
        }
        
        private IEnumerable<LotteryWinnerData> GetLotteryWinnerList(int siteId ,int roundId)
        {
            return _eventsService.GetLotteryWinnerList(siteId , roundId);
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

        public IEnumerable<EventRestriction> GetEventRestriction(int siteId)
        {
            var restrictionList = _eventsService.GetEventRestriction(siteId);
            return restrictionList;
        }
        
        public async Task<int> BuyChineseZodiacSticker(Customer custInfo,int roundId,ChineseZodiacStickerId stickerId,decimal stickerPrize)
        {
            var custLotteryTicketRequest = new CustLotteryTicketInsertRequest
            {
                CustId = custInfo.CustId,
                TicketNo = "",
                RoundId = roundId,
                StickerId = stickerId,
                Type = 2
            };
            
            var insertResult = _eventsService.InsertCustLotteryTicket(custLotteryTicketRequest ,stickerPrize);

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
                StickerId = stickerId.ToString(),
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

