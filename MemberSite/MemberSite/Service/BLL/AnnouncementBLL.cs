using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class AnnouncementBLL : IAnnouncementBLL
    {
        private readonly IAnnouncementService _announcementService;
        private readonly IHostEnvironment _environment;
        private const int LimitRow = 15;
        public AnnouncementBLL(IHostEnvironment environment, IAnnouncementService announcementService)
        {
            _environment = environment;
            _announcementService = announcementService;
        }

        public async Task<IEnumerable<Announcement>> GetAnnouncements(int siteId, LanguageObject language)
        {
            var announcements = await _announcementService.GetAnnouncement(siteId, LimitRow, !_environment.IsProduction());
            foreach (var announcement in announcements)
            {
                announcement.Subject = JsonContentParser.Parse(announcement.Subject, language.LanguageCode, language.DefaultLanguage);
                announcement.Content = JsonContentParser.Parse(announcement.Content, language.LanguageCode, language.DefaultLanguage);
            }

            return announcements;
        }

    }
}
