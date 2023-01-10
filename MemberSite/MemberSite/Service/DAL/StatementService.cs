using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;
using System.Data;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{

    public class StatementService
    {
        private readonly CacheHelper _cacheHelper;
        private readonly MySqlClient _reportDb;
        
        public StatementService(IServiceProvider provider)
        {
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
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
            return _reportDb.Query<TpList>("Member_TpList_Get");
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
            return _reportDb.Query<GameTypeList>("Common_TpGameType_Get");
        }

        public IEnumerable<TpBetRecordListOneDay> GetCustTpBetRecordListOneDay(int custId, string selDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            var SpName = "Member_TpBetRecord_byOneDay_Get_V2";
            var props = new DynamicParameters(new { CustId = custId, SelDate = selDate });
            props.Add("@PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            props.Add("@TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@PageSize", PageSize);

            var result = _reportDb.Query<TpBetRecordListOneDay>(SpName, props);
            PageNumber = props.Get<int?>("@PageNumber") ?? 0;
            TotalPages = props.Get<int?>("@TotalPages") ?? 0;
            RowCount = props.Get<int?>("@RowCount") ?? 0;
            
            return result;
        }
        
        public IEnumerable<TpBetRecordbyCategoryId> GetCustTpBetRecordListbyCategoryId(int custId, string selDate, int categoryId, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            var SpName = "Member_TpBetRecord_byCategoryId_Get_V2";
            var props = new DynamicParameters(new { CustId = custId, SelDate = selDate, CategoryId = categoryId });
            props.Add("@PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            props.Add("@TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@PageSize", PageSize);

            var result = _reportDb.Query<TpBetRecordbyCategoryId>(SpName, props);
            PageNumber = props.Get<int?>("@PageNumber") ?? 0;
            TotalPages = props.Get<int?>("@TotalPages") ?? 0;
            RowCount = props.Get<int?>("@RowCount") ?? 0;
            
            return result;
        }
        
        public IEnumerable<TpBetRecordDetailbyGameType> GetCustTpBetRecordDetailListbyGameType(int custId, string selDate, int gameTypeId, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            var SpName = "Member_TpBetRecord_byGameTypeList_Get_V2";
            var props = new DynamicParameters(new { CustId = custId, SelDate = selDate, TypeId = gameTypeId });
            props.Add("@PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            props.Add("@TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@PageSize", PageSize);

            var result = _reportDb.Query<TpBetRecordDetailbyGameType>(SpName, props);
            PageNumber = props.Get<int?>("@PageNumber") ?? 0;
            TotalPages = props.Get<int?>("@TotalPages") ?? 0;
            RowCount = props.Get<int?>("@RowCount") ?? 0;
            
            return result;
        }
        
        public IEnumerable<TpBetRecordListDateRange> GetCustTpBetRecordDateRange(int custId, string startDate, string endDate, int PageSize, ref int PageNumber, ref int TotalPages, ref int RowCount)
        {
            var SpName = "Member_TpBetRecord_byDateRange_Get_V2";
            var props = new DynamicParameters(new { CustId = custId, StartDate = startDate, EndDate = endDate });
            props.Add("@PageNumber", PageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            props.Add("@TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);
            props.Add("@PageSize", PageSize);

            var result = _reportDb.Query<TpBetRecordListDateRange>(SpName, props);
            PageNumber = props.Get<int?>("@PageNumber") ?? 0;
            TotalPages = props.Get<int?>("@TotalPages") ?? 0;
            RowCount = props.Get<int?>("@RowCount") ?? 0;
            
            return result;
        }

        public IEnumerable<CryptoInfo> GetSupportCryptoInfo()
        {
            var result = _reportDb.Query<CryptoInfo>("Member_SupportCryptoInfo_Get");
            
            return result;
        }
    }
}
