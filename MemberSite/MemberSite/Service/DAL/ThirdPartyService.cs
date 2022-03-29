using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class ThirdPartyService
    {
        //private static ConcurrentDictionary<int, IThirdPartyHelper> ThirdPartyHelperContainer = new ConcurrentDictionary<int, IThirdPartyHelper>();
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;

        public ThirdPartyService(IServiceProvider provider)
        {
            environment = provider.GetRequiredService<IWebHostEnvironment>();
            configuration = provider.GetRequiredService<IConfiguration>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public ThirdPartyModel GetThirdPartyBySite(int tpId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<ThirdPartyModel>("Common_Tp_Get", new { TpId = tpId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        /*public IThirdPartyHelper GetThirdPartyHelper(int tpId)
        {
            if (!ThirdPartyHelperContainer.ContainsKey(tpId))
            {
                var oThirdPartyModel = GetThirdPartyModel(tpId);
                var assembly = Assembly.Load($"ThirdParty.{oThirdPartyModel.TpName}");
                var assemblyType = assembly.GetType($"ThirdParty.{oThirdPartyModel.TpName}.TpHelper");
                //var oThirdPartyHelper = new Lazy<IThirdPartyHelper>(() => (IThirdPartyHelper)Activator.CreateInstance(assemblyType));
                //if (oThirdPartyHelper.IsValueCreated)
                ThirdPartyHelperContainer.TryAdd(tpId, (IThirdPartyHelper)Activator.CreateInstance(assemblyType));
                //ThirdPartyHelperContainer[apiType].InsertLog += InsertLog;
                //oThirdPartyModel.ApiName
            }
            return ThirdPartyHelperContainer[tpId];
        }*/

        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<TpCurrency>("TpCurrency_Get", new { TpId = tpId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<TpWallet> GetTpWallet(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<TpWallet>("Member_TpWallet_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<TpTransferInfoHistory> GetTransferHistory(int custId, long startDate, long endDate, int siteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate, SiteId = siteId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var tpTransferInfoHistoryResult = conn.Query<TpTransferInfoHistory>("Member_TpTransferInfoSearch_Get", p, commandType: CommandType.StoredProcedure);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return tpTransferInfoHistoryResult;
        }
        public IEnumerable<TpTransferInfoHistory> GetTransferRecent(int custId, int siteId, int topNum)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<TpTransferInfoHistory>("Member_TpTransferInfoRecent_Get", new { CustId = custId, SiteId = siteId, TopNum = topNum }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<TpWalletList> GetTpWalletList(int custId,int listType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<TpWalletList>("Member_TpWalletV2_Get", new { CustId = custId, ListType = listType }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<TpMaintain> GetTpMaintainList(int siteId)
        {
            return _mainDbClient.Query<TpMaintain>("TpMaintain_Get", new DynamicParameters(new { SiteId = siteId }));
        }
    }
}
