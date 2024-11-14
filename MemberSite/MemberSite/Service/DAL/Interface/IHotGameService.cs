using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IHotGameService
    {
        /// <summary>
        /// 取得 HotGame 資料清單
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="currencyId"></param>
        /// <returns></returns>
        Task<IEnumerable<HotGameDto>> GetHotGames(int siteId, int currencyId, GetHotGameDataListRequest request);
    }
}
