using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.DAL
{
    public class CurrencyService
    {
        private readonly MySqlClient _reportDbClient;

        public CurrencyService(IEnumerable<MySqlClient> mySqlClients)
        {
            _reportDbClient = mySqlClients.FirstOrDefault(a => a.DataBase == Model.enums.DataBase.Report);
        }

        public async Task<string> GetTimeZone(int currencyId)
        {
            var currency = await _reportDbClient.QueryFirstAsync<Currency>("Member_Currency_Get", new { _CurrencyId = currencyId });
            return currency.TimeZone;
        }
    }
}
