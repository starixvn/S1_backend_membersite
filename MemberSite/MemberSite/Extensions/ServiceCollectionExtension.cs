using MemberSite.Service.Library;
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
    }
}