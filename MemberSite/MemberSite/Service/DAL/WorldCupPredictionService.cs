using System.Data;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class WorldCupPredictionService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;

        public WorldCupPredictionService(IServiceProvider provider)
        {
            this._configuration = provider.GetRequiredService<IConfiguration>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main); ;
        }

        /// <summary>
        /// 取得後台使用者設定的世界盃設定
        /// </summary>
        /// <param name="langId">語系Id，看前端傳什語系</param>
        /// <returns></returns>
        public IEnumerable<WorldCupPredictionSettingDto> GetWorldCupPredictionSetting(string langId)
        {
            var p = new DynamicParameters();
            p.Add("LangId", langId, dbType: DbType.String);
            return _mainDbClient.Query<WorldCupPredictionSettingDto>("Admin_WorldCupPredictionSetting_Get", p);
        }

        /// <summary>
        /// 取得世界盃所有國家 Id 與英文名稱
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WorldCupPredictionOptionsDto> GetWorldCupPredictionOptions()
        {
            return _mainDbClient.Query<WorldCupPredictionOptionsDto>("Admin_WorldCupPredictionOptions_Get");
        }
    }
}
