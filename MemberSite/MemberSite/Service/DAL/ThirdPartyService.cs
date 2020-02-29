using Dapper;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.DAL
{
    public class ThirdPartyService
    {
        //private static ConcurrentDictionary<int, IThirdPartyHelper> ThirdPartyHelperContainer = new ConcurrentDictionary<int, IThirdPartyHelper>();
        private static ConcurrentDictionary<string, ThirdPartyModel> ThirdPartyModelContainer = new ConcurrentDictionary<string, ThirdPartyModel>();
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;

        public ThirdPartyService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            this.environment = environment;
            this.configuration = configuration;
        }

        public ThirdPartyModel GetThirdPartyBySite(int tpId, int siteId)
        {
            string sKet = $"{tpId}_{siteId}";
            if (!ThirdPartyModelContainer.ContainsKey(sKet))
            {
                using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                var thirdPartyModel = conn.QueryFirstOrDefault<ThirdPartyModel>("Common_Tp_Get", new { TpId = tpId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
                if (thirdPartyModel != null)
                    ThirdPartyModelContainer.TryAdd(sKet, thirdPartyModel);
            }

            return ThirdPartyModelContainer.ContainsKey(sKet) ? ThirdPartyModelContainer[sKet] : null;
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

        public string GetTpLanId(int tpId, string langId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<string>("TpLangId_Get", new { TpId = tpId, LangId = langId }, commandType: CommandType.StoredProcedure);
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCurrency>("TpCurrency_Get", new { TpId = tpId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<TpWallet> GetTpWallet(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<TpWallet>("Member_TpWallet_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
    }
}
