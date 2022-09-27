using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Interface
{
    public interface IHotGameBLL
    {
        /// <summary>
        /// 取得 HotGame 資料清單
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="currencyId"></param>
        /// <param name="deviceType"></param>
        /// <returns></returns>
        Task<Dictionary<string, IEnumerable<HotGameResponse>>> GetHotGames(int siteId, int currencyId, int deviceType);

    }
}
