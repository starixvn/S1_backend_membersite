using System.Collections.Generic;
using System.Threading.Tasks;
using MemberSite.Service.Model;

namespace MemberSite.Service.DAL.Interface
{
    public interface IServerInfoService
    {
        ServerInfo GetModel(int siteId);
        IEnumerable<Country> GetCountryList();
        int GetSite(string domain);
        SeoModel GetSeo(string domain);
        IEnumerable<Domainlist> GetSupportDomainList(int siteId, int currencyId);
        Task<IEnumerable<MenuOfGameData>> GetMenuOfGames(int siteId, string langId);
    }
}