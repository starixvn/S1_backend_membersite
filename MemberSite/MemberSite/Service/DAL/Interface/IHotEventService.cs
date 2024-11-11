using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IHotEventService
    {
        /// <summary>
        /// 取得HotEvent列表
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="langId"></param>
        /// <returns></returns>
        Task<IEnumerable<HotEventDto>> GetHotEventDataList(int siteId, string langId);

        Task<IEnumerable<HotMatchDto>> GetHotMatchDataList();

        /// <summary>
        /// 取得聯賽隊伍資訊，langId = 'en'
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SportTeamsInfoDto>> GetSportTeamsInfoList();

        /// <summary>
        /// 取得聯賽資訊，langId = 'en'
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SportLeaguesInfoDto>> GetSportLeaguesInfoList();
    }
}
