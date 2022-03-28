using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
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
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? config.GetValue<string>("ServiceType");

            return services
               .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_MainDB"), logger, DataBase.Main))
               .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_ReportDB"), logger, DataBase.Report))
               .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_WagerDB"), logger, DataBase.Wager))
               .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_LogDB"), logger, DataBase.Log));
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
