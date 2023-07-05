using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Lobby;

namespace MemberSite.Service.BLL
{
    public class LobbyCategoryBLL
    {
        private readonly ILogger<LobbyCategoryBLL> _logger;
        private readonly ICacheHelper _cacheHelper;
        private readonly LobbyCategoryService _lobbyCategoryService;

        public LobbyCategoryBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<LobbyCategoryBLL>>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _lobbyCategoryService = provider.GetRequiredService<LobbyCategoryService>();
        }

        /// <summary>
        /// 初始大廳選單分類列表
        /// </summary>
        public async Task<List<LobbyCategorySequenceInfo>> GetLobbyCategoryClassifyList()
        {
            var key = $"GetLobbyCategoryClassifyList";
            var result = await _cacheHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromDays(1),
                async () =>
                {
                    var result = await _lobbyCategoryService.GetLobbyCategoryClassifyList();
                    return result;
                });
            return result.ToList();
        }

        /// <summary>
        /// 大廳選單分類列表
        /// </summary>
        public async Task<List<LobbyCategorySequenceInfo>> GetLobbyCategorySequenceListBySiteId(int siteId)
        {
            var key = $"GetLobbyCategorySequenceListBySiteId:{siteId}";
            var result = await _cacheHelper.TryGetOrCreateAsync(key,
                TimeSpan.FromDays(1),
                async () =>
                {
                    var result = await _lobbyCategoryService.GetLobbyCategorySequenceList(siteId);
                    return result;
                });
            return result.ToList();
        }

        /// <summary>
        /// 依條件取得大廳選單分類 (SiteId、CurrencyId、LobbyCategoryId)
        /// </summary>
        public async Task<List<LobbyCategorySequenceInfo>> GetLobbyCategorySequenceByLobbyCategoryId(int siteId, int currencyId, LobbyCategory lobbyCategory)
        {
            // 初始資料
            if (siteId == 0 || currencyId == 0)
            {
                var initial = await GetLobbyCategoryClassifyList();
                return initial.Where(x => x.LobbyCategoryId == (int)lobbyCategory).ToList();
            }

            // 依條件資料
            var list = await GetLobbyCategorySequenceListBySiteId(siteId);
            return list.Where(x => x.SiteId == siteId && x.CurrencyId == currencyId && x.LobbyCategoryId == (int)lobbyCategory).ToList();
        }
    }
}
