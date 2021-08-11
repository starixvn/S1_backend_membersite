using System;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using MemberSite.Service.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemberSite.Service.DAL
{
    public class WithdrawalLimitService
    {
        private readonly MySqlClient _mainDbClient;

        public WithdrawalLimitService(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILogger<MySqlClient>>();
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);
        }
        public int SetWithdrawalLimit(int custId, string userName, int siteId, decimal withdrawalLimit, string memo, string refTransId, out decimal afterWithdrawalLimit, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { CustId = custId, UserName = userName, SiteId = siteId, WithdrawalLimit = withdrawalLimit, Memo = memo, RefTransId = refTransId });
            p.Add("AfterWithdrawalLimit", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Common_WithdrawalLimit_Set", p, conn, trans);
            afterWithdrawalLimit = p.Get<decimal?>("AfterWithdrawalLimit") ?? 0;
            return result;
        }
    }
}
