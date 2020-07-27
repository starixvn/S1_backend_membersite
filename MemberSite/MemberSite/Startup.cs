using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.WebEncoders;

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

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MailConfig>(configuration.GetSection("MailConfig"));

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
            services.AddSingleton<MessageService>();
            services.AddSingleton<ThirdPartyService>();
            services.AddSingleton<PayService>();
            services.AddSingleton<RolloverService>();
            services.AddSingleton<PromotionService>();
            services.AddSingleton<CasinoService>();
            services.AddSingleton<StatementService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<BannerService>();
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
                option.Cookie.SameSite = SameSiteMode.None;
            });
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
                                      builder.WithOrigins("http://localhost:8000", "http://localhost:3000", "http://10.168.11.11:8080", "http://10.168.11.14:8080", "https://m.nova88.net")
                                      .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowCredentials();
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
