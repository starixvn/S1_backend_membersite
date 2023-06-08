using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Lobby;

namespace MemberSite.Service.DAL
{
    public class LobbyCategoryService
    {
        private readonly MySqlClient _reportDb;
        public LobbyCategoryService(IServiceProvider provider)
        {
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public async Task<IEnumerable<LobbyCategorySequenceInfo>> GetLobbyCategorySequenceList(int siteId)
        {
            return await _reportDb.QueryAsync<LobbyCategorySequenceInfo>("Member_LobbyCategorySequence_BySiteId_Get", new
            {
                _SiteId = siteId
            });
        }

        public async Task<IEnumerable<LobbyCategorySequenceInfo>> GetLobbyCategoryClassifyList()
        {
            return await _reportDb.QueryAsync<LobbyCategorySequenceInfo>("Member_LobbyCategoryClassify_Get", null);
        }
    }
}
