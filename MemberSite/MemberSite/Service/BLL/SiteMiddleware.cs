using MemberSite.Service.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class SiteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SelLangBLL selLangBLL;
        private readonly Tools tools;
        private readonly IConfiguration configuration;

        public SiteMiddleware(IConfiguration configuration, RequestDelegate next, SelLangBLL selLangBLL, Tools tools)
        {
            _next = next;
            this.selLangBLL = selLangBLL;
            this.tools = tools;
            this.configuration = configuration;
        }
        public RouteValueDictionary Match(string routeTemplate, string requestPath)
        {
            var template = TemplateParser.Parse(routeTemplate);
            var matcher = new TemplateMatcher(template, GetDefaults(template));
            RouteValueDictionary values = new RouteValueDictionary();
            matcher.TryMatch(requestPath, values);
            return values;
        }
        private RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
        {
            var result = new RouteValueDictionary();
            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    result.Add(parameter.Name, parameter.DefaultValue);
                }
            }
            return result;
        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                if (IsBlockIP(context))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return;
                }
                else if (IsGetMethod(context) && IsSupportContentType(context))
                {
                    await _next.Invoke(context);
                    if (context.Response.StatusCode == 404)
                    {
                        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                        {
                            var AnticipationRouteData = Match("{controller=Home}/{action=Index}", context.Request.Path);
                            object controller = "";
                            object action = "";

                            if (AnticipationRouteData.TryGetValue("controller", out controller) && AnticipationRouteData.TryGetValue("action", out action) && controller as string != selLangBLL.GetLangId())
                            {
                                context.Response.Redirect($"/{selLangBLL.GetLangId()}/{controller}/{action}{context.Request.QueryString.Value}");
                                return;
                            }
                        }
                    }
                }
                else
                {
                    await _next.Invoke(context);
                }

            }
            catch (Exception)
            {
                throw;
            }
        }
        private bool IsGetMethod(HttpContext context)
        {
            return string.Equals("GET", context.Request.Method, StringComparison.OrdinalIgnoreCase);
        }
        private bool IsSupportContentType(HttpContext context)
        {
            return true;
        }
        private bool IsBlockIP(HttpContext context)
        {
            string[] IPs = configuration.GetSection("BlockIP").Get<string[]>();
            if (IPs == null)
            {
                return true;
            }
            else
            {
                var clientIP = tools.GetClientIP(context);
                return IPs.Any(x => x == clientIP);
            }
        }
    }
}
