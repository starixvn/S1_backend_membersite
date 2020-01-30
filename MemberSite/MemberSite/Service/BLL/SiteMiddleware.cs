using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class SiteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SelLangBLL selLangBLL;

        public SiteMiddleware(RequestDelegate next, SelLangBLL selLangBLL)
        {
            _next = next;
            this.selLangBLL = selLangBLL;
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
                if (IsGetMethod(context) && IsSupportContentType(context))
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
    }
}
