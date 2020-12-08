using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;

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
        private readonly string AllowSpecificOrigins = "AllowAllOriginsPolicy";
        private IConfiguration configuration;
        private readonly IHostEnvironment environment;

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

            services.AddMemoryCache();
            /*Library*/
            services.AddSingleton<Tools>();
            services.AddSingleton<Common>();
            services.AddSingleton<CourierAdapter>();
            services.AddSingleton<RegexUtilities>();
            services.AddSingleton<IBCDBLibrary>();
            services.AddSingleton<CodeGenerator>();

            /*DAL*/
            services.AddSingleton<ServerInfoService>();
            services.AddSingleton<AgentInfoService>();
            services.AddSingleton<CustInfoService>();
            services.AddSingleton<AuthenticateService>();
            services.AddSingleton<DepoistService>();
            services.AddSingleton<WithdrawalService>();
            services.AddSingleton<BankInfoOffService>();
            services.AddSingleton<BankInfoService>();
            services.AddSingleton<WalletService>();
            services.AddSingleton<BonusWalletService>();
            services.AddSingleton<WithdrawalLimitService>();
            services.AddSingleton<MessageService>();
            services.AddSingleton<ThirdPartyService>();
            services.AddSingleton<PayService>();
            services.AddSingleton<RolloverService>();
            services.AddSingleton<PromotionService>();
            services.AddSingleton<CasinoService>();
            services.AddSingleton<StatementService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<BannerService>();
            services.AddSingleton<BonusWalletService>();
            services.AddSingleton<NewsService>();
            services.AddSingleton<IovationService>();
            services.AddSingleton<EvnetsService>();            
            // foreach (var s in AppDomain.CurrentDomain.GetAssemblies()
            //.SelectMany(t => t.GetTypes())
            //.Where(t => t.IsClass && t.Namespace == "MemberSite.Service.DAL"))
            // {
            //     services.AddSingleton(s);
            // }

            /*BLL*/
            services.AddSingleton<ServerInfoBLL>();
            services.AddSingleton<CustInfoBLL>();
            services.AddSingleton<WalletBLL>();
            /*??SelLangBLL?Scope*/
            services.AddSingleton<SelLangBLL>();
            services.AddSingleton<AuthorizeBLL>();
            services.AddSingleton<BankInfoOffBLL>();
            services.AddSingleton<BankInfoBLL>();
            services.AddSingleton<MobilePayInfoBLL>();
            services.AddSingleton<CryptoPayInfoBLL>();
            services.AddSingleton<LoadCardPayInfoBLL>();
            services.AddSingleton<DepositBLL>();
            services.AddSingleton<PaymentBLL>();
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<MessageBLL>();
            services.AddSingleton<PromotionBLL>();
            services.AddSingleton<GamingBLL>();
            services.AddSingleton<CasinoBLL>();
            services.AddSingleton<CommonBLL>();
            services.AddSingleton<TemplateBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddSingleton<BannerBLL>();
            services.AddSingleton<BonusWalletBLL>();
            services.AddSingleton<NewsBLL>();
            services.AddSingleton<IovationBLL>();
            services.AddSingleton<FPSBLL>();
            services.AddSingleton<EventsBLL>();
            //var bll = AppDomain.CurrentDomain.GetAssemblies()
            //.SelectMany(t => t.GetTypes())
            //.Where(t => t.IsClass && t.Namespace == "MemberSite.Service.BLL");

            //foreach (var s in bll)
            //{
            //    services.AddSingleton(s);
            //}

            services.AddSingleton<CustomCookieAuthenticationEvents>();
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();

            if (configuration.GetValue<bool>("JwtSettings:IsJwt"))
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
                           ValidIssuer = configuration.GetValue<string>("JwtSettings:Issuer"),
                           // 通常不太需要驗證 Audience
                           ValidateAudience = false,
                           //ValidAudience = "JwtAuthDemo", // 不驗證就不需要填寫
                           // 一般我們都會驗證 Token 的有效期間
                           ValidateLifetime = true,
                           // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
                           ValidateIssuerSigningKey = true,
                           // "1234567890123456" 應該從 IConfiguration 取得
                           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("JwtSettings:SignKey"))),
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
                    option.ExpireTimeSpan = TimeSpan.FromMinutes(30);//沒給預設14天
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
                options.AddPolicy(name: AllowSpecificOrigins,
                                  builder =>
                                  {
                                      builder.WithOrigins("http://localhost:8000", "http://localhost:3000", "http://10.168.11.11:8080", "http://10.168.11.14:8080", "https://m.nova88.net", "http://stag1.bwing.com")
                                      .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowCredentials();
                                  });

                options.AddPolicy("LandingPage",
                            builder =>
                            {
                                builder.WithOrigins("http://www.bwing.com")
                                    .AllowAnyMethod();
                            });

            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors(AllowSpecificOrigins);
            app.UseCors("LandingPage");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<SiteMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers();
                //endpoints.MapAreaControllerRoute("areaRoute", "{area:exists}", "{area:exists}/{controller=Home}/{action=Index}");
                endpoints.MapAreaControllerRoute(name: "areaRoute", "{area:exists}", "{LangId?}/{area:exists}/{controller=Home}/{action=Index}");
                endpoints.MapControllerRoute(
                    name: "desktop",
                //pattern: "{controller=Home}/{action=Index}");
                pattern: "{LangId?}/{controller=Home}/{action=Index}");
            });
        }
    }
}

