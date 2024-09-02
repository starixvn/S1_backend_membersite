using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface;

public interface IOlympicEventBLL
{
    public Task<OlympicMemberDepositTicketInfoResponse> GetOlympicEventTicket(int siteId, int custId, string userName, string langId, int currencyId);
    public Task<ClaimOlympicEventPrizeResponse> ClaimOlympicEventPrize(int siteId, int custId, string userName, int currencyId,int usedGoldMetalCount);
    public string GetOlympicsList();
}