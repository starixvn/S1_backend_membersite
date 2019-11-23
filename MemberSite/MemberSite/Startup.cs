using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MemberSite
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            /*Library*/
            services.AddSingleton<Tools>();
            services.AddSingleton<Common>();
            services.AddSingleton<IBCDBLibrary>();

            /*DAL*/
            services.AddSingleton<ServerInfoService>();
            services.AddSingleton<AgentInfoService>();
            services.AddSingleton<CustInfoService>();
            services.AddSingleton<AuthenticateService>();
            services.AddSingleton<DepoistService>();
            services.AddSingleton<BankInfoService>();
            services.AddSingleton<WalletService>();

            /*BLL*/
            services.AddSingleton<ServerInfoBLL>();
            services.AddSingleton<CustInfoBLL>();
            services.AddSingleton<WalletBLL>();
            services.AddScoped<SelLangBLL>();

            services.AddControllersWithViews();
            services.AddHttpContextAccessor();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(option =>
            {
                //或許要從組態檔讀取，自己斟酌決定
                //option.LoginPath = new PathString("/Home/Login");//登入頁
                //option.LogoutPath = new PathString("/Home/Logout");//登出Action
                //用戶頁面停留太久，登入逾期，或Controller中用戶登入時機點也可以設定↓
                option.Cookie.Name = ".ES";
                option.ExpireTimeSpan = TimeSpan.FromMinutes(30);//沒給預設14天
            });
            services.AddDataProtection().DisableAutomaticKeyGeneration();
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("CookieKeys")).SetApplicationName("SharedCookieApp");
            //services.ConfigureApplicationCookie(options => { options.Cookie.Name = ".AspNet.SharedCookie"; });
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

            app.UseAuthentication();
            //app.UseAuthorization();//Controller、Action才能加上 [Authorize] 屬性

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers();
                endpoints.MapAreaControllerRoute("areaRoute", "{area:exists}", "{area:exists}/{controller=Home}/{action=Index}");
                //endpoints.MapAreaControllerRoute("areaRoute", "{area:exists}", "{area:exists}/{controller=Home}/{action=Index}/{LandId}");
                endpoints.MapControllerRoute(
                    name: "desktop",
                    pattern: "{controller=Home}/{action=Index}");
                //pattern: "{controller=Home}/{action=Index}/{LandId?}");
            });
        }
    }
}
