using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

namespace MemberSite
{
    public class Program
    {
        private static IConfigurationRoot Configuration => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile($"appsettings.json", false, true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", false, true)
            .Build();
        private static readonly List<string> Excludes = new List<string>()
        {
            "/health",
            "{HostingRequestStartingLog:l}",
            "Executing endpoint '{EndpointName}'",
            "Route matched with {RouteData}.",
            "Executing ObjectResult, writing value of type '{Type}'",
            "Executed action {ActionName}",
            "Executed endpoint '{EndpointName}'",
            "{HostingRequestFinishedLog:l}",
            "Executing ContentResult with HTTP Response ContentType of {ContentType}",
            "CORS policy execution successful.",
            "CORS policy execution failed.",
            "Request origin {origin} does not have permission to access the resource.",
            "Authorization was successful.",
            "Executed ViewResult - view {ViewName} executed in {ElapsedMilliseconds}ms.",
            "Sending file. Request path: '{VirtualPath}'. Physical path: '{PhysicalPath}'",
            "Executing ViewResult, running view {ViewName}."
        };
        
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    
                    webBuilder.UseSerilog((hostContext, loggerConfiguration) =>
                        {
                            loggerConfiguration
                                .ReadFrom.Configuration(Configuration)
                                .Enrich.FromLogContext()
                                .Filter.ByExcluding(c => c.Properties.Any(p => p.Value.ToString().Contains("/health")
                                    || p.Value.ToString().Contains("/api/Account/onlineCheck")
                                    || p.Value.ToString().Contains("/api/Message/GetAnnouncement")
                                    || Excludes.Any(item =>  c.MessageTemplate.ToString().Contains(item))))
                                .Enrich.WithExceptionDetails()
                                .MinimumLevel.Information().WriteTo.Console(new JsonFormatter());

                        })
                        .UseStartup<Startup>();
                });
    }
}
