using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using MemberSite.Extensions;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model.Mapping;
using StackExchange.Redis;
using MemberSite.Service.Model.Mapping;

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
        private readonly IConfiguration configuration;
        private readonly IHostEnvironment environment;
        private static IConfigurationRoot DomainConfiguration => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"domain.json", false, true)
            .Build();

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            this.configuration = configuration;
            this.environment = environment;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MailConfig>(configuration.GetSection("MailConfig"));
            services.Configure<JwtConfig>(configuration.GetSection("JwtSettings"));
            services.Configure<LineLoginChannelConfig>(configuration.GetSection("LineLoginChannelConfig"));
            services.Configure<LineLoginConfig>(configuration.GetSection("LineLoginConfig"));
            services.Configure<ShieldConfig>(configuration.GetSection("ShieldConfig"));
            services.Configure<RecaptchaSetting>(configuration.GetSection("ReCaptchaSetting"));
            services.Configure<Dictionary<string, SiteConfig>>(configuration.GetSection("SiteConfig"));
            services.Configure<Dictionary<string, string>>(configuration.GetSection("LoginDeviceConfig"));
            services.Configure<AllowIpList>(configuration.GetSection("AllowIpList"));
            services.Configure<Dictionary<string, AWSKeyConfig>>(configuration.GetSection("AwsKeyConfig"));
            services.AddMemoryCache();
            services.AddDatabaseConnection(configuration);
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetValue<string>("RedisConnection")));
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
            services.AddSingleton<StatementService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<IBannerService, BannerService>();
            services.AddSingleton<BonusWalletService>();
            services.AddSingleton<NewsService>();
            services.AddSingleton<IovationService>();
            services.AddSingleton<EventsService>();
            services.AddSingleton<GameCasinoService>();
            services.AddSingleton<LiveStreamingService>();
            services.AddSingleton<LatestHighlightDataService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<AppLinkService>();
            services.AddSingleton<DeepLinkService>();
            services.AddSingleton<IVerificationRewardService, VerificationRewardService>();
            services.AddSingleton<ICustPhotoIdService, CustPhotoIdService>();
            services.AddTransient<WorldCupPredictionService>();
            services.AddTransient<RecaptchaService>();
            services.AddSingleton<PartitionService>();

            services.AddSingleton<IPService>();
            services.AddSingleton<ShieldService>();
            services.AddTransient<IHotEventService, HotEventService>();

            services.AddTransient<IHotGameService, HotGameService>();
            
            /*BLL*/
            services.AddSingleton<ServerInfoBll>();
            services.AddSingleton<CustInfoBll>();
            services.AddSingleton<WalletBLL>();
            /*??SelLangBLL?Scope*/
            services.AddSingleton<SelLangBLL>();
            services.AddSingleton<AuthorizeBLL>();
            services.AddSingleton<BankInfoOffBLL>();
            services.AddSingleton<BankInfoBLL>();
            services.AddSingleton<MobilePayInfoBll>();
            services.AddSingleton<CryptoPayInfoBll>();
            services.AddSingleton<LoadCardPayInfoBLL>();
            services.AddSingleton<DepositBLL>();
            services.AddSingleton<PaymentBLL>();
            services.AddSingleton<ThirdPartyBll>();
            services.AddSingleton<MessageBll>();
            services.AddSingleton<PromotionBll>();
            services.AddSingleton<RedemptionBLL>();
            services.AddSingleton<StoreBLL>();
            services.AddSingleton<GamingBLL>();
            services.AddSingleton<CasinoBll>();
            services.AddSingleton<CommonBLL>();
            services.AddSingleton<TemplateBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddSingleton<BannerBLL>();
            services.AddSingleton<BonusWalletBLL>();
            services.AddSingleton<NewsBLL>();
            services.AddSingleton<IovationBLL>();
            services.AddSingleton<FPSBLL>();
            services.AddSingleton<EventsBLL>();
            services.AddSingleton<GameCasinoBll>();
            services.AddSingleton<LiveStreamingBll>();
            services.AddSingleton<LatestHighlightDataBll>();
            services.AddSingleton<GameBLL>();
            services.AddTransient<IGeoIpApi, GeoIpApi>();
            services.AddTransient<IRecaptchaApi, RecaptchaApi>();
            services.AddTransient<ICustPhotoIdBLL, CustPhotoIdBLL>();
            services.AddTransient<GeoIpBLL>();
            services.AddTransient<AppLinkBLL>();
            services.AddTransient<FileNameBLL>();
            services.AddTransient<ImageEditorBLL>();
            services.AddTransient<VerificationRewardBLL>();
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
            
            services.AddSingleton<CustomCookieAuthenticationEvents>();
            services.AddTransient<HotEventBLL>();
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();

            if (configuration.GetValue<bool>("JwtSettings:IsJwt"))
            {
                services
                   // �ˬd HTTP Header �� Authorization �O�_�� JWT Bearer Token
                   .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                   // �]�w JWT Bearer Token ���ˬd�ﶵ
                   .AddJwtBearer(options =>
                   {
                       // �����ҥ��ѮɡA�^�����Y�|�]�t WWW-Authenticate ���Y�A�o�̷|��ܥ��Ѫ��Բӿ��~��]
                       options.IncludeErrorDetails = true;
                       // �������һݭnHTTPS
                       options.RequireHttpsMetadata = true;
                       // ���NJWT�X�ݥO�P�O�s�b��eHttpContext�A�H�K�ڭ̥i�H�ϥΤ�kawait HttpContext.GetTokenAsync(��Bearer��, ��access_token��)��������k�˯����C
                       options.SaveToken = true;

                       options.TokenValidationParameters = new TokenValidationParameters
                       {
                           // �@��ڭ̳��|���� Issuer
                           ValidateIssuer = true,
                           ValidIssuer = configuration.GetValue<string>("JwtSettings:Issuer"),
                           // �q�`���ӻݭn���� Audience
                           ValidateAudience = false,
                           //ValidAudience = "JwtAuthDemo", // �����ҴN���ݭn��g
                           // �@��ڭ̳��|���� Token �����Ĵ���
                           ValidateLifetime = true,
                           // �p�G Token ���]�t key �~�ݭn���ҡA�@�볣�u��ñ���Ӥw
                           ValidateIssuerSigningKey = true,
                           // "1234567890123456" ���ӱq IConfiguration ���o
                           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("JwtSettings:SignKey"))),
                       };
                   });
            }
            else
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(option =>
                {
                    //�γ\�n�q�պA��Ū���A�ۤv�r�u�M�w
                    //option.LoginPath = new PathString("/Home/Login");//�n�J��
                    //option.LogoutPath = new PathString("/Home/Logout");//�n�XAction
                    //�Τ᭶�����d�Ӥ[�A�n�J�O���A��Controller���Τ�n�J�ɾ��I�]�i�H�]�w��
                    option.LoginPath = "/";
                    option.Cookie.Name = ".ES";
                    option.ExpireTimeSpan = TimeSpan.FromMinutes(30);//�S���w�]14��
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

            services.AddOptionListByDB<BlockIP>("Member_BlockIP_Get");

            services.AddOptionListByDB<AreaCodeMap>("Common_CountryList_Get");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();

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
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                });
                endpoints.MapAreaControllerRoute(name: "areaRoute", "{area:exists}", "{LangId?}/{area:exists}/{controller=Home}/{action=Index}");
                endpoints.MapControllerRoute(
                    name: "desktop",
                pattern: "{LangId?}/{controller=Home}/{action=Index}");
            });
        }
    }
}

