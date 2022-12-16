using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;
using System.Data;
using MemberSite.Service.Library;

namespace MemberSite.Service.DAL
{

    public class StatementService
    {
        private readonly IConfiguration _configuration;
        private readonly CacheHelper _cacheHelper;
        
        public StatementService(IServiceProvider provider)
        {
            _configuration = provider.GetRequiredService<IConfiguration>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        public IEnumerable<TpList> GetTpListWithCache()
        {
            var key = $"CustInfo_TpList";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)),
                i => GetTpList());
        }
        public IEnumerable<TpList> GetTpList()
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<TpList>("Member_TpList_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<GameTypeList> GetGameTypeListWithCache()
        {
            var key = $"CustInfo_GameType";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(60)),
                i => GetGameTypeList());
        }
        public IEnumerable<GameTypeList> GetGameTypeList()
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<GameTypeList>("Common_TpGameType_Get", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<TpBetRecordListOneDay> GetCustTpBetRecordListOneDay(int custId, string selDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            IEnumerable<TpBetRecordListOneDay> result = null;
            using (var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport")))
            {
                var SpName = "Member_TpBetRecord_byOneDay_Get_V2";
                var props = new DynamicParameters(new { CustId = custId, SelDate = selDate });
                props.Add("@PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
                props.Add("@TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@PageSize", PageSize);

                result = conn.Query<TpBetRecordListOneDay>(SpName, props, commandType: CommandType.StoredProcedure);
                PageNumber = props.Get<int?>("@PageNumber") ?? 0;
                TotalPages = props.Get<int?>("@TotalPages") ?? 0;
                RowCount = props.Get<int?>("@RowCount") ?? 0;
            }
            return result;
        }
        public IEnumerable<TpBetRecordbyCategoryId> GetCustTpBetRecordListbyCategoryId(int custId, string selDate, int categoryId, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {

            IEnumerable<TpBetRecordbyCategoryId> result = null;
            using (var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport")))
            {
                var SpName = "Member_TpBetRecord_byCategoryId_Get_V2";
                var props = new DynamicParameters(new { CustId = custId, SelDate = selDate, CategoryId = categoryId });
                props.Add("@PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
                props.Add("@TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@PageSize", PageSize);

                result = conn.Query<TpBetRecordbyCategoryId>(SpName, props, commandType: CommandType.StoredProcedure);
                PageNumber = props.Get<int?>("@PageNumber") ?? 0;
                TotalPages = props.Get<int?>("@TotalPages") ?? 0;
                RowCount = props.Get<int?>("@RowCount") ?? 0;
            }
            return result;
        }
        public IEnumerable<TpBetRecordDetailbyGameType> GetCustTpBetRecordDetailListbyGameType(int custId, string selDate, int gameTypeId, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            IEnumerable<TpBetRecordDetailbyGameType> result = null;
            using (var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport")))
            {
                var SpName = "Member_TpBetRecord_byGameTypeList_Get_V2";
                var props = new DynamicParameters(new { CustId = custId, SelDate = selDate, TypeId = gameTypeId });
                props.Add("@PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
                props.Add("@TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@PageSize", PageSize);

                result = conn.Query<TpBetRecordDetailbyGameType>(SpName, props, commandType: CommandType.StoredProcedure);
                PageNumber = props.Get<int?>("@PageNumber") ?? 0;
                TotalPages = props.Get<int?>("@TotalPages") ?? 0;
                RowCount = props.Get<int?>("@RowCount") ?? 0;
            }
            return result;
        }
        public IEnumerable<TpBetRecordListDateRange> GetCustTpBetRecordDateRange(int custId, string startDate, string endDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            IEnumerable<TpBetRecordListDateRange> result = null;
            using (var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport")))
            {
                var SpName = "Member_TpBetRecord_byDateRange_Get_V2";
                var props = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate });
                props.Add("@PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
                props.Add("@TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
                props.Add("@PageSize", PageSize);

                result = conn.Query<TpBetRecordListDateRange>(SpName, props, commandType: CommandType.StoredProcedure);
                PageNumber = props.Get<int?>("@PageNumber") ?? 0;
                TotalPages = props.Get<int?>("@TotalPages") ?? 0;
                RowCount = props.Get<int?>("@RowCount") ?? 0;
            }
            return result;
        }

        public IEnumerable<CryptoInfo> GetSupportCryptoInfo(int siteId)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            var result = conn.Query<CryptoInfo>(
                "Member_SupportCryptoInfo_Get_V2", new { _SiteId = siteId },
                commandType: CommandType.StoredProcedure);
            
            return result;
        }
    }
}
