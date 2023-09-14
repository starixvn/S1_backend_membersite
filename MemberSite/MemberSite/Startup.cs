using System.Text;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Tokens;

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
            services.Configure<MailConfig>(_configuration.GetSection("MailConfig"));
            services.Configure<JwtConfig>(_configuration.GetSection("JwtSettings"));
            services.Configure<LineLoginChannelConfig>(_configuration.GetSection("LineLoginChannelConfig"));
            services.Configure<LineLoginConfig>(_configuration.GetSection("LineLoginConfig"));
            services.Configure<ShieldConfig>(_configuration.GetSection("ShieldConfig"));
            services.Configure<RecaptchaSetting>(_configuration.GetSection("ReCaptchaSetting"));
            services.Configure<Dictionary<string, SiteConfig>>(_configuration.GetSection("SiteConfig"));
            services.Configure<Dictionary<string, string>>(_configuration.GetSection("LoginDeviceConfig"));
            services.Configure<AllowIpList>(_configuration.GetSection("AllowIpList"));
            services.Configure<Dictionary<string, AWSKeyConfig>>(_configuration.GetSection("AwsKeyConfig"));
            services.AddMemoryCache();
            services.AddDatabaseConnection(_configuration);
            services.AddMessageQueueConnection(_configuration);
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
            services.AddTransient<IAffiliateService, AffiliateService>();
            services.AddTransient<GameService>();

            services.AddSingleton<IPService>();
            services.AddSingleton<ShieldService>();
            services.AddTransient<IHotEventService, HotEventService>();

            services.AddTransient<IHotGameService, HotGameService>();
            services.AddTransient<IAnnouncementService, AnnouncementService>();
            services.AddSingleton<VIPService>();

            /*BLL*/
            services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
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
            services.AddSingleton<IPBLL>();
            services.AddSingleton<ShieldBLL>();
            services.AddTransient<WorldCupPredictionBLL>();
            services.AddSingleton<SlotGameBLL>();
            services.AddTransient<RecaptchaBLL>();
            services.AddTransient<CheckAttackBLL>();
            services.AddTransient<DateTimeHelper>();
            services.AddTransient<IAnnouncementBLL, AnnouncementBLL>();

            services.AddTransient<IHotGameBLL, HotGameBLL>();

            services.AddSingleton<CustomCookieAuthenticationEvents>();
            services.AddTransient<HotEventBLL>();
            services.AddSingleton<CryptoHelper>();
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();

            if (_configuration.GetValue<bool>("JwtSettings:IsJwt"))
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
                           ValidIssuer = _configuration.GetValue<string>("JwtSettings:Issuer"),
                           // 通常不太需要驗證 Audience
                           ValidateAudience = false,
                           //ValidAudience = "JwtAuthDemo", // 不驗證就不需要填寫
                           // 一般我們都會驗證 Token 的有效期間
                           ValidateLifetime = true,
                           // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
                           ValidateIssuerSigningKey = true,
                           // "1234567890123456" 應該從 IConfiguration 取得
                           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("JwtSettings:SignKey"))),
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

            services.AddDataProtection().DisableAutomaticKeyGeneration();
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("CookieKeys")).SetApplicationName("SharedCookieApp");

            //services.ConfigureApplicationCookie(options =>
            //{
            //    options.EventsType = typeof(CustomCookieAuthenticationEvents);
            //});
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
            services.AddOptionListByDB<AreaCodeMap>("Common_CountryList_Get");
            services.AddOptionListByDB<SabaSportTypeOption>("Service_SabaSportType_Get");
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

            app.UseCors();
            app.UseCors("LandingPage");
            app.UseCors("SocialChannel");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRequestLogMiddleware();
            app.UseResponseLogMiddleware();
            app.UseMiddleware<SiteMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}");
                });
                endpoints.MapControllerRoute(name: "desktop", pattern: "{LangId?}/{controller=Home}/{action=Index}");
            });
        }
    }
}

