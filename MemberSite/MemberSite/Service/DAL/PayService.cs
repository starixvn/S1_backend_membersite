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
    public class PayService
    {
        private readonly IConfiguration configuration;
        public PayService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<PayCurrency>("PayCurrency_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<MobilePayInfo> GetMobilePayInfo(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<MobilePayInfo>("Member_MobilePayV2_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<CryptoPayInfo> GetCryptoPayInfo(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<CryptoPayInfo>("Member_CryptoPay_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
