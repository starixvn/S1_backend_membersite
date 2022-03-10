using Dapper;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;

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
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<PayCurrency>("PayCurrency_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<MobilePayInfo> GetMobilePayInfo(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<MobilePayInfo>("Member_MobilePayV2_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<CryptoPayInfo> GetCryptoPayInfo(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<CryptoPayInfo>("Common_CryptoPay_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfo(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<LoadCardPayInfo>("Common_LoadCardPay_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<PayInfo> GetPayInfo(int siteId, DepositType depositType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<PayInfo>("Service_PayInfo_Get", new { SiteId = siteId , DepositType = (int)depositType }, commandType: CommandType.StoredProcedure);
        }

        public LineChannel? GetLineChannel(int siteId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<LineChannel?>("Member_LineChannel_Get", new { _SiteId = siteId, _CurrencyId = (int)currencyId }, commandType: CommandType.StoredProcedure);
        }
    }
}
