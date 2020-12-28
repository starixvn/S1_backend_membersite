using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.DAL
{
    public class BannerService
    {
        private readonly IConfiguration configuration;
        public BannerService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        //public IEnumerable<Banner> GetBanner(int SiteId, string FolderName, string Lang, int CurrencyId)
        //{
        //    using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
        //    return conn.Query<Banner>("Member_Banner_Get", new { SiteId, FolderName, Lang, CurrencyId }, commandType: CommandType.StoredProcedure);
        //}
        public IEnumerable<BannerV2> GetBannerV2(int SiteId, int BannerType, int ShowStatus, int CurrencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BannerV2>("Member_Banner_Get", new { SiteId, BannerType, ShowStatus, CurrencyId}, commandType: CommandType.StoredProcedure);
        }
    }
}
