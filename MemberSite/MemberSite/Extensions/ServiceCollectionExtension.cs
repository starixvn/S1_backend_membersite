using MemberSite.Service.Library;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MemberSite.Service.DAL;
using System.Collections.Generic;
using System.Linq;

namespace MemberSite.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var provider = services.BuildServiceProvider();

            var mainConn = config.GetConnectionString("ConnectionString");
            var reportConn = config.GetConnectionString("ConnectionStringReport");

            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            services.AddSingleton(serviceProvider => new MySqlClient(mainConn, logger)
            {
                Name = "MainDB"
            });
            services.AddSingleton(serviceProvider => new MySqlClient(reportConn, logger)
            {
                Name = "ReportDB"
            });

            return services;
        }
    
        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }

        public static IServiceCollection AddOptionByDB<T>(this IServiceCollection services, string sql, object parameters = null)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters).FirstOrDefault();
            services.AddSingleton(typeof(T), p => option);
            return services;
        }
    }
}
