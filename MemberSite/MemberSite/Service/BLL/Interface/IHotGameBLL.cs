using System.Security.Claims;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

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
        Task<Dictionary<string, IEnumerable<HotGameResponse>>> GetHotGames(int siteId, int currencyId, HotGameDeviceType deviceType, string langId, GetHotGameDataListRequest request, ClaimsPrincipal user);
        Task<IEnumerable<ProviderResponse>> GetProvider();

    }
}
