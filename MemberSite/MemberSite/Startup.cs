using System.Text.Encodings.Web;
using System.Text.Unicode;
using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.SqlClient;
using Elastic.Apm.StackExchange.Redis;
using MemberSite.Extensions;
using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.Mapping;
using MemberSite.Service.Model.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.WebEncoders;
using Prometheus;
using StackExchange.Redis;

namespace MemberSite
{
    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        public override Task RedirectToLogin(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> ctx)
        {
            if (ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
                ctx.Response.StatusCode == (int)System.Net.HttpStatusCode.OK)
            {
                ctx.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
            }
            else
            {
                ctx.Response.Redirect(ctx.RedirectUri);
            }
            return Task.CompletedTask;
        }
    }
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private static IConfigurationRoot DomainConfiguration => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"domain.json", false, true)
            .Build();

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddDatabaseConnection(_configuration);
            
            var redisConnection = ConnectionMultiplexer.Connect(_configuration.GetValue<string>("RedisConnection"));
            redisConnection.UseElasticApm();
            services.AddSingleton<IConnectionMultiplexer>(redisConnection);
            services.AddSingleton<ICacheHelper, RedisCacheHelper>();

            /*Library*/
            services.AddSingleton<Tools>();
            services.AddSingleton<Common>();
            services.AddSingleton<CourierAdapter>();
            services.AddSingleton<CodeGenerator>();
            services.AddTransient<JsonNetSerializer>();
            services.AddSingleton<IHttpHelper, HttpHelper>();
            services.AddSingleton<CacheHelper>();
            services.AddSingleton<TpGameTypeHelper>();
            services.AddSingleton<IpHelper>();
            services.AddSingleton<AWSS3Helper>();
            services.AddSingleton<StringVaildHelper>();

            /*DAL*/
            services.AddSingleton<IServerInfoService, ServerInfoService>();
            services.AddSingleton<AgentInfoService>();
            services.AddSingleton<CustInfoService>();
            services.AddSingleton<AuthenticateService>();
            services.AddSingleton<DepositService>();
            services.AddSingleton<WithdrawalService>();
            services.AddSingleton<BankInfoOffService>();
            services.AddSingleton<BankInfoService>();
            services.AddSingleton<WalletService>();
            services.AddSingleton<BonusWalletService>();
            services.AddSingleton<WithdrawalLimitService>();
            services.AddSingleton<MessageService>();
            services.AddSingleton<ThirdPartyService>();
            services.AddSingleton<PayService>();
            services.AddSingleton<PromotionService>();
            services.AddSingleton<CasinoService>();
            services.AddSingleton<LobbyCategoryService>();
            services.AddSingleton<StatementService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<IBannerService, BannerService>();
            services.AddSingleton<BonusWalletService>();
            services.AddSingleton<NewsService>();
            services.AddSingleton<IovationService>();
            services.AddSingleton<EventsService>();
            services.AddTransient<IEventsService, EventsService>();            
            services.AddSingleton<LiveStreamingService>();
            services.AddSingleton<VideoDataService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<AppLinkService>();
            services.AddSingleton<DeepLinkService>();
            services.AddSingleton<IVerificationRewardService, VerificationRewardService>();
            services.AddSingleton<ICustPhotoIdService, CustPhotoIdService>();
            services.AddTransient<WorldCupPredictionService>();
            services.AddTransient<RecaptchaService>();
            services.AddSingleton<PartitionService>();
            services.AddTransient<CurrencyService>();
            services.AddTransient<GameService>();
            services.AddTransient<IAffiliateService, AffiliateService>();
            services.AddTransient<QueueJobService>();

            services.AddSingleton<IPService>();
            services.AddSingleton<ShieldService>();
            services.AddTransient<IHotEventService, HotEventService>();

            services.AddTransient<IHotGameService, HotGameService>();
            services.AddTransient<IAnnouncementService, AnnouncementService>();
            services.AddSingleton<VIPService>();
            services.AddSingleton<BigWinService>();

            /*BLL*/
            services.AddSingleton<ServerInfoBll>();
            services.AddSingleton<CustInfoBll>();
            services.AddSingleton<WalletBLL>();
            /*??SelLangBLL?Scope*/
            services.AddSingleton<SelLangBLL>();
            services.AddSingleton<AuthorizeBLL>();
            services.AddSingleton<BankInfoOffBLL>();
            services.AddSingleton<BankInfoBLL>();
            services.AddSingleton<CryptoPayInfoBll>();
            services.AddSingleton<LoadCardPayInfoBLL>();
            services.AddSingleton<DepositBLL>();
            services.AddSingleton<DepositLimitBLL>();
            services.AddSingleton<PaymentBLL>();
            services.AddSingleton<ThirdPartyBll>();
            services.AddSingleton<MessageBll>();
            services.AddSingleton<PromotionBll>();
            services.AddSingleton<RedemptionBLL>();
            services.AddSingleton<StoreBLL>();
            services.AddSingleton<CasinoBll>();
            services.AddSingleton<LobbyCategoryBLL>();
            services.AddSingleton<CommonBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddSingleton<BannerBLL>();
            services.AddSingleton<BonusWalletBLL>();
            services.AddSingleton<NewsBLL>();
            services.AddSingleton<IovationBLL>();
            services.AddSingleton<FPSBLL>();
            services.AddSingleton<EventsBLL>();
            services.AddTransient<AngPaoProcessor>();
            services.AddTransient<IAngPaoBLL, AngPaoBLL>();
            services.AddSingleton<LiveStreamingBll>();
            services.AddSingleton<VideoDataBll>();
            services.AddSingleton<GameBLL>();
            services.AddTransient<IGeoIpApi, GeoIpApi>();
            services.AddTransient<IRecaptchaApi, RecaptchaApi>();
            services.AddTransient<ICustPhotoIdBLL, CustPhotoIdBLL>();
            services.AddTransient<GeoIpBLL>();
            services.AddTransient<AppLinkBLL>();
            services.AddTransient<FileNameBLL>();
            services.AddTransient<ImageEditorBLL>();
            services.AddSingleton<VerificationRewardBLL>();
            services.AddTransient<DeepLinkBLL>();
            services.AddSingleton<PayInfoBLL>();
            services.AddSingleton<LineLoginBLL>();
            services.AddSingleton<SiteBll>();
            services.AddTransient<WorldCupPredictionBLL>();
            services.AddSingleton<IPBLL>();
            services.AddSingleton<ShieldBLL>();
            services.AddTransient<WorldCupPredictionBLL>();
            services.AddSingleton<SlotGameBLL>();
            services.AddTransient<IHotGameBLL, HotGameBLL>();
            services.AddTransient<RecaptchaBLL>();
            services.AddTransient<CheckAttackBLL>();
            services.AddTransient<DateTimeHelper>();
            services.AddTransient<IAnnouncementBLL, AnnouncementBLL>();

            services.AddTransient<IHotGameBLL, HotGameBLL>();
            services.AddTransient<BigWinBLL>();
            services.AddSingleton<CustomCookieAuthenticationEvents>();
            services.AddTransient<HotEventBLL>();
            services.AddTransient<NewYear2024BLL>();
            services.AddTransient<QueueJobBLL>();
            services.AddSingleton<CryptoHelper>();
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();

            services.Configure<WebEncoderOptions>(options =>
            {
                options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            });

            services.AddCors(options =>
            {
                var allowCORSDomain = DomainConfiguration.GetSection("AllowCORSDomain").Get<string[]>();
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins(allowCORSDomain)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .SetIsOriginAllowedToAllowWildcardSubdomains();
                    });

                options.AddPolicy("SocialChannel",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .SetIsOriginAllowedToAllowWildcardSubdomains();
                    });

                options.AddPolicy("LeaderBoard",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .SetIsOriginAllowedToAllowWildcardSubdomains();
                    });


                options.AddPolicy("LandingPage",
                    builder =>
                    {
                        builder.WithOrigins("http://www.bwing.com", "https://www.n88dev.com", "https://m.n88dev.com",
                                "http://www.n88dev.com", "http://m.n88dev.com",
                                "http://stag.bwing.com", "http://stag.goal123.com")
                            .AllowAnyMethod();
                    });
            });

            services.AddControllers();

            services.AddOptionListByDB<TpGameTypeOption>("Admin_TpGameType_Get");

            services.AddOptionListByDB<BlockIP>("Member_BlockIP_Get");

            services.AddOptionListByDB<AreaCodeMap>("Common_CountryList_Get");
            services.AddOptionListByDB<SabaSportTypeOption>("Service_SabaSportType_Get");

            //處理SiteConfig
            services.AddOptionListByDB<ServerInfo>("ServerInfoList_Get");
            services.AddOptionListByDB<ConfigObject>("SiteConfig_Get", new { ConfigGroup = "MemberSite" });
            services.AddSiteConfig();
            services.AddSingleton<ConfigBLL>();
            services.AddGoogleReCaptchaSetting();

            // JWT | Cookie Auth
            services.AddAuthenticationDefinition();
            services.AddDataProtection().DisableAutomaticKeyGeneration();
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("CookieKeys")).SetApplicationName("SharedCookieApp");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();
            if (!env.IsEnvironment("Local"))
            {
                app.UseElasticApm(_configuration,
                    new HttpDiagnosticsSubscriber(),
                    new SqlClientDiagnosticSubscriber());
            }
            app.UseStaticFiles();

            app.UseRouting();
            app.UseHttpMetrics(options =>
            {
                options.AddCustomLabel("host", context => context.Request.Host.Host);
            });
            app.UseCors();
            app.UseCors("LandingPage");
            app.UseCors("SocialChannel");
            app.UseCors("LeaderBoard");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRequestLogMiddleware();
            app.UseResponseLogMiddleware();
            app.UseMiddleware<SiteMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}");
                });
                endpoints.MapControllerRoute(name: "desktop", pattern: "{LangId?}/{controller=Home}/{action=Index}");
            });
        }
    }
}

