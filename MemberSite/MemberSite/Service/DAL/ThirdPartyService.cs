using System.Data;

using Dapper;

using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class ThirdPartyService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDb;

        public ThirdPartyService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public ThirdPartyModel GetThirdPartyBySite(int tpId, int siteId)
        {
            return _reportDb.QueryFirst<ThirdPartyModel>("Common_Tp_Get", new { TpId = tpId, SiteId = siteId });
        }

        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            return _reportDb.QueryFirst<TpCurrency>("TpCurrency_Get", new { TpId = tpId, CurrencyId = currencyId });
        }

        public IEnumerable<TpWallet> GetTpWallet(int custId)
        {
            return _reportDb.Query<TpWallet>("Member_TpWallet_Get", new { CustId = custId });
        }

        public IEnumerable<TpTransferInfoHistory> GetTransferHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var p = new DynamicParameters(new
            {
                _CustId = custId,
                _StartDate = startDate,
                _EndDate = endDate,
                _SiteId = siteId,
                _PageSize = pageSize
            });
            p.Add("_PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("_TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("_RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("_StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var tpTransferInfoHistoryResult = _reportDb.Query<TpTransferInfoHistory>("Member_TpTransferInfoSearch_Get_V3", p);
            totalPages = p.Get<int?>("_TotalPages") ?? 0;
            rowCount = p.Get<int?>("_RowCount") ?? 0;

            return tpTransferInfoHistoryResult;
        }

        public IEnumerable<TpTransferInfoHistory> GetTransferRecent(int custId, int siteId, int topNum)
        {
            return _reportDb.Query<TpTransferInfoHistory>("Member_TpTransferInfoRecent_Get_V2", new { CustId = custId, SiteId = siteId, TopNum = topNum });
        }

        public IEnumerable<TpWalletList> GetTpWalletList(int custId, int listType)
        {
            return _reportDb.Query<TpWalletList>("Member_TpWallet_Get_V3", new { CustId = custId, ListType = listType });
        }

        public IEnumerable<TpMaintain> GetTpMaintainList(int siteId)
        {
            return _mainDbClient.Query<TpMaintain>("TpMaintain_Get", new DynamicParameters(new { SiteId = siteId }));
        }
    }
}
