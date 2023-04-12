using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IAnnouncementService
    {
        Task<IEnumerable<Announcement>> GetAnnouncement(int siteId, int limitRow, bool isTest);
    }
}