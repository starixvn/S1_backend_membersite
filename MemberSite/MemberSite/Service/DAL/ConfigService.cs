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
using System.Linq;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class ConfigService
    {
        private readonly MySqlClient _reportDbClient;

        public ConfigService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null)
        {
            return _reportDbClient.Query<T>(sql, parameters);
        }
    }
}
