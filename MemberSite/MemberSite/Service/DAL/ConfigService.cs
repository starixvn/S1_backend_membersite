using System;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using System.Data;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace MemberSite.Service.DAL
{
    public class ConfigService
    {
        private readonly MySqlClient _reportDbClient;

        public ConfigService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<MySqlClient>>();
            _reportDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger);
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null)
        {
            return _reportDbClient.Query<T>(sql, parameters);
        }
    }
}
