using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class BigWinService
    {
        private readonly MySqlClient _reportDbClient;

        public BigWinService(IEnumerable<MySqlClient> mySqlClients)
        {
            _reportDbClient = mySqlClients.First(p => p.DataBase == DataBase.Report);
        }

        public async Task<IEnumerable<BigWin>> GetBigWinPopUpDataList(int siteId, int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            return await _reportDbClient.QueryAsync<BigWin>("Member_BigWinInfo_Get", p);
        }
    }
}
