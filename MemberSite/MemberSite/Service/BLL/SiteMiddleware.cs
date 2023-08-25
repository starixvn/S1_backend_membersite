using System.Net;

using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Caching.Memory;

namespace MemberSite.Service.BLL
{
    public class SiteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SelLangBLL _selLangBll;
        private readonly Tools _tools;
        private readonly IConfiguration _configuration;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly IPBLL _iPBLL;
        private readonly CacheHelper _cacheHelper;

        public SiteMiddleware(IServiceProvider provider, RequestDelegate next)
        {
            _next = next;
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _iPBLL = provider.GetRequiredService<IPBLL>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }
        public RouteValueDictionary Match(string routeTemplate, string requestPath)
        {
            var template = TemplateParser.Parse(routeTemplate);
            var matcher = new TemplateMatcher(template, GetDefaults(template));
            var values = new RouteValueDictionary();
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
                    var host = context.Request.Host.Host;
                    var supportH5 = _configuration.GetSection("SupportH5").Get<int[]>();

                    if (!IPAddress.TryParse(host, out var pAddress) && supportH5 != null && _tools.IsMobile() && supportH5.Any(x => x == _serverInfoBll.GetServerInfo().SiteId) && !host.StartsWith("m.", StringComparison.OrdinalIgnoreCase) && !host.StartsWith("mrelease.", StringComparison.OrdinalIgnoreCase))
                    {
                        var AnticipationRouteData = Match("{controller=Home}/{action=Index}", context.Request.Path);
                        AnticipationRouteData.TryGetValue("controller", out var controller);
                        AnticipationRouteData.TryGetValue("action", out var action);

                        var AnticipationRouteData1 = Match("{LangId?}/{controller=Home}/{action=Index}", context.Request.Path);
                        AnticipationRouteData1.TryGetValue("controller", out var controller1);
                        AnticipationRouteData1.TryGetValue("action", out var action1);

                        var dotIndex = host.IndexOf(".");
                        if (dotIndex > -1)
                        {
                            host = host.Substring(dotIndex + 1);
                        }

                        if ((controller != null && controller.ToString().Equals("Home", StringComparison.OrdinalIgnoreCase)) || (controller1 != null && controller1.ToString().Equals("Home", StringComparison.OrdinalIgnoreCase)))
                        {
                            if ((action != null && action.ToString().Equals("Register", StringComparison.OrdinalIgnoreCase)) || (action1 != null && action1.ToString().Equals("Register", StringComparison.OrdinalIgnoreCase)))
                            {
                                context.Response.Redirect($"{context.Request.Scheme}://m.{host}/join{context.Request.QueryString.Value}");
                            }
                            else
                            {
                                context.Response.Redirect($"{context.Request.Scheme}://m.{host}{context.Request.QueryString.Value}");
                            }
                            return;
                        }
                    }

                    await _next.Invoke(context);
                    if (context.Response.StatusCode == 404)
                    {
                        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                        {
                            var AnticipationRouteData = Match("{controller=Home}/{action=Index}", context.Request.Path);

                            if (AnticipationRouteData.TryGetValue("controller", out var controller) && AnticipationRouteData.TryGetValue("action", out var action) && controller as string != _selLangBll.GetLangId())
                            {
                                context.Response.Redirect($"/{_selLangBll.GetLangId()}/{controller}/{action}{context.Request.QueryString.Value}");
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
            var key = $"GetBlockIP";
            var _blockIp = _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)),
                i => _iPBLL.GetBlockIp<BlockIP>());
            if (_blockIp == null || !_blockIp.Any())
            {
                return false;
            }

            var clientIp = _tools.GetClientIP(context);
            return _blockIp.Any(x => x.IP == clientIp);
        }
    }
}
