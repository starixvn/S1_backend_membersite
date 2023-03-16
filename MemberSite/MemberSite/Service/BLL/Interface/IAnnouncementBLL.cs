using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface
{
    public interface IAnnouncementBLL
    {
        Task<IEnumerable<Announcement>> GetAnnouncements(int siteId, LanguageObject language);
    }
}