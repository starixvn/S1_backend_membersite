using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface
{
    public interface IAngPaoBLL
    {
        Task<(int errorCode, int ticketCount)> CheckDrawPrize(int custId, int currencyId);
        Task<(int error, Prize prize)> DrawPrize(int eventId, Customer customer);
        Task<IEnumerable<CustGiftList>> GetTodayCustDrawPrizeList(int eventId, int custId, int currencyId);
    }
}