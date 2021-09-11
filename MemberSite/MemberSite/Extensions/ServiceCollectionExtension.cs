using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemberSite.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();

            services.AddSingleton(provider => new MySqlClient(config.GetConnectionString("ConnectionString"), logger)
            {
                DataBase = DataBase.Main
            });
            services.AddSingleton(provider => new MySqlClient(config.GetConnectionString("ConnectionStringReport"), logger)
            {
                DataBase = DataBase.Report
            });
            services.AddSingleton(provider => new MySqlClient(config.GetConnectionString("ConnectionStringWager"), logger)
            {
                DataBase = DataBase.Wager
            });
            services.AddSingleton(provider => new MySqlClient(config.GetConnectionString("ConnectionStringLog"), logger)
            {
                DataBase = DataBase.Log
            });

            return services;
        }
    }
}