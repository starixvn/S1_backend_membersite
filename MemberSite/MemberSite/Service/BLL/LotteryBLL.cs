using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;

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
                StickerId = 0,
                StickerIsPurchase = false,
                CustTurnoverProccessing = 0,
                CustTurnoverSummary = 0
            };
            
            var lotteryRoundInfo = GetLotteryRoundInfo(siteId, DateTime.UtcNow);
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
            var custStickerId = originTicketList.FirstOrDefault(x=>x.Type == (int)LotteryTicketEnum.Sticker);
            var stickerId = custStickerId?.StickerId ?? 0; 
            
            var turnoverSummary = _eventsService.GetCustLotteryTurnOverSummary(custId, lotteryRoundInfo.StartDate.UtcDateTime, lotteryRoundInfo.EndDate.UtcDateTime);
            var proccessing = turnoverSummary - setting.TurnOver * custTicketList.Count;
            
            lotteryInfo.LotteryRoundInfo = lotteryRoundInfo;
            lotteryInfo.LotteryTierList = tierCurrencyList;
            lotteryInfo.TicketNo = custTicketList;
            lotteryInfo.StickerId = stickerId;
            lotteryInfo.StickerIsPurchase = stickerId != 0;
            lotteryInfo.CustTurnoverSummary = turnoverSummary;
            lotteryInfo.CustTurnoverProccessing = proccessing;
            errorCode = 0;
            
            return (lotteryInfo , errorCode);
        }
        
        public LotteryRoundInfo GetLotteryRoundInfo(int siteId, DateTime date)
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
    }
}

