using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class AngPaoBLL : IAngPaoBLL
    {
        private readonly IEventsService _eventsService;
        private readonly AngPaoProcessor _angPaoProcessor;

        public AngPaoBLL(IEventsService eventsService, AngPaoProcessor angPaoProcessor)
        {
            _eventsService = eventsService;
            _angPaoProcessor = angPaoProcessor;
        }

        public async Task<(int errorCode, int ticketCount)> CheckDrawPrize(int custId, int currencyId)
        {
            var ticketCount = 0;
            var tickets = await _eventsService.GetTickets(custId, currencyId);
            if (!tickets.Any()) return (102, ticketCount);

            ticketCount = tickets.Count(a => a.IsUsed == false);
            if (ticketCount == 0) return (103, ticketCount);

            return (0, ticketCount);
        }

        public async Task<(int error, Prize prize)> DrawPrize(int eventId, Customer customer)
        {
            var res = 0;
            var drawPrize = new Prize();

            if (string.IsNullOrEmpty(customer.AccountName) || !customer.PhoneVerified) return (104, null);

            //檢查今日有無票
            var tickets = await _eventsService.GetTickets(customer.CustId, customer.CurrencyId);

            if (!tickets.Any()) return (102, null);

            var ticket = tickets.FirstOrDefault(a => a.IsUsed == false);
            if (ticket == null) return (103, null);

            var prizes = await _eventsService.GetPrizes(eventId, customer.CurrencyId);
            var todayPrize = await _eventsService.GetCustPrizeOnToday(eventId, customer.CustId, customer.CurrencyId);
            var isUsed = true;
            // 如果今日已中獎後面給空紅包
            if (todayPrize != null)
            {
                drawPrize = prizes.SingleOrDefault(a => a.Amount == 0);
                isUsed = false;
            }
            else
            {
                drawPrize = _angPaoProcessor.Drawing(prizes);
            }
            res = await _eventsService.PayoutPrize(eventId, drawPrize.PrizeId, drawPrize.PrizeName, customer.SiteId, customer.CustId, customer.UserName, customer.CurrencyId, drawPrize.Amount, ticket.TicketNo, drawPrize.PrizeType, isUsed);
            return (res, drawPrize);
        }

        public async Task<IEnumerable<CustGiftList>> GetTodayCustDrawPrizeList(int eventId, int custId, int currencyId)
        {
            return await _eventsService.GetTodayCustDrawPrizeList(eventId, custId, currencyId);
        }
    }
}
