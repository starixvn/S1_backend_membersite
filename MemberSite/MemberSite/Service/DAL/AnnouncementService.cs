using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.DAL
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly MySqlClient _reportClient;

        public AnnouncementService(IEnumerable<MySqlClient> dbClient)
        {
            _reportClient = dbClient.First(a => a.DataBase == Model.enums.DataBase.Report);
        }

        public async Task<IEnumerable<Announcement>> GetAnnouncement(int siteId, int limitRow, bool isTest)
        {
            return await _reportClient.QueryAsync<Announcement>("Member_Announcement_Get_V2", new { _LimitRow = limitRow, _IsTest = isTest, _SiteId = siteId });
        }
    }
}
