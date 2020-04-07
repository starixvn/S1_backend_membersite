using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class CasinoBLL
    {
        private readonly CasinoService casinoService;
        public CasinoBLL(CasinoService casinoService)
        {
            this.casinoService = casinoService;
        }

        public IEnumerable<MemberFavorite> GetFavoriteGames(int CustId)
        {
            return casinoService.GetFavoriteGames(CustId);
        }

        public void SetFavoriteGames(int CustId, int ProviderId, string GameId, bool IsRemove)
        {
            casinoService.SetFavoriteGames(CustId, ProviderId, GameId, IsRemove);
        }

        public IEnumerable<int> GetProviderIdByCurrency(int CurrencyId)
        {
            return casinoService.GetProviderIdByCurrency(CurrencyId);
        }
    }
}
