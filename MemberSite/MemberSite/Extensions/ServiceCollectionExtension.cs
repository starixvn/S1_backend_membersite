using System.Text;

using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MemberSite.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? config.GetValue<string>("ServiceType");
            var reportDBConn = config.GetConnectionString($"{serviceType}_ReportDB");
            var reportBackupDB1 = config.GetConnectionString($"{serviceType}_ReportBackupDB1");
            var reportBackupDB2 = config.GetConnectionString($"{serviceType}_ReportBackupDB2");

            //檢查是否有BackupDB連線字串
            if (!string.IsNullOrEmpty(reportBackupDB1) && !string.IsNullOrEmpty(reportBackupDB2))
            {
                // 設定ReportDB連線順序
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.Report, reportDBConn, new List<string>()
                {
                    reportDBConn,   //1b
                    reportBackupDB1,//1c
                    reportBackupDB2,//1a
                }));

                // 設定ReportBackupDB連線順序
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.ReportBackupDB, reportBackupDB1, new List<string>()
                {
                    reportBackupDB1,//1c
                    reportDBConn,   //1b
                    reportBackupDB2,//1a
                }));
            }
            else
            {
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.Report, reportDBConn));
            }

            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Main, config.GetConnectionString($"{serviceType}_MainDB")));
            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Log, config.GetConnectionString($"{serviceType}_LogDB")));
            services.AddSingleton(provider => new MySqlClient(provider, DataBase.ReportIPTable, config.GetConnectionString($"{serviceType}_ReportIPTable")));
            return services;
        }

        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null, DataBase dataBase = DataBase.Report)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters, dataBase);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }

        public static IServiceCollection AddSiteConfig(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            // 準備ServerInfo
            var serverInfoList = new List<ServerInfo>() { new ServerInfo() { SiteId = 0, SiteName = "default" } };
            serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

            // 準備Config
            var siteConfigGroup = provider.GetRequiredService<IEnumerable<ConfigObject>>().GroupBy(i => i.SiteId);

            foreach (var serverInfo in serverInfoList)
            {
                //設定SiteConfig
                var siteConfig = siteConfigGroup.FirstOrDefault(i => i.Key == serverInfo.SiteId) ?? siteConfigGroup.First(i => i.Key == 0);
                var config = siteConfig.ToDictionary(i => i.ConfigKey, i => i.ConfigValue);
                services.AddSingleton(new SiteConfigObject(serverInfo, config));
            }

            return services;
        }

        public static IServiceCollection AddGoogleReCaptchaSetting(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            var recaptchaSettings = configBll.GetConfigValue<RecaptchaSetting>("ReCaptchaSetting");
            services.AddSingleton(recaptchaSettings);
            return services;
        }

        public static IServiceCollection AddAuthenticationDefinition(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            var jwtSettings = configBll.GetConfigValueByEnvironment<JwtConfig>("JwtSettings");

            if (jwtSettings.IsJwt)
            {
                services
                   // 檢查 HTTP Header 的 Authorization 是否有 JWT Bearer Token
                   .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                   // 設定 JWT Bearer Token 的檢查選項
                   .AddJwtBearer(options =>
                   {
                       // 當驗證失敗時，回應標頭會包含 WWW-Authenticate 標頭，這裡會顯示失敗的詳細錯誤原因
                       options.IncludeErrorDetails = true;
                       // 身份驗證需要HTTPS
                       options.RequireHttpsMetadata = true;
                       // 它將JWT訪問令牌保存在當前HttpContext，以便我們可以使用方法await HttpContext.GetTokenAsync(“Bearer”, “access_token”)或類似方法檢索它。
                       options.SaveToken = true;

                       options.TokenValidationParameters = new TokenValidationParameters
                       {
                           // 一般我們都會驗證 Issuer
                           ValidateIssuer = true,
                           ValidIssuer = jwtSettings.Issuer,
                           // 通常不太需要驗證 Audience
                           ValidateAudience = false,
                           //ValidAudience = "JwtAuthDemo", // 不驗證就不需要填寫
                           // 一般我們都會驗證 Token 的有效期間
                           ValidateLifetime = true,
                           // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
                           ValidateIssuerSigningKey = true,
                           // "1234567890123456" 應該從 IConfiguration 取得
                           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SignKey)),
                       };
                   });
            }
            else
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(option =>
                {
                    //或許要從組態檔讀取，自己斟酌決定
                    //option.LoginPath = new PathString("/Home/Login");//登入頁
                    //option.LogoutPath = new PathString("/Home/Logout");//登出Action
                    //用戶頁面停留太久，登入逾期，或Controller中用戶登入時機點也可以設定↓
                    option.LoginPath = "/";
                    option.Cookie.Name = ".ES";
                    option.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    option.EventsType = typeof(CustomCookieAuthenticationEvents);
                    //option.Cookie.SameSite = environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Unspecified;
                    option.Cookie.SameSite = SameSiteMode.Unspecified;
                });
            }

            return services;
        }
    }
}
