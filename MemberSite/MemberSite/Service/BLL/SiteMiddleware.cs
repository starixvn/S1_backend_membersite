using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
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

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
                if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {

                    if (context.Response.StatusCode == 404)
                    {
                        context.Response.Redirect($"/Home/Index/{selLangBLL.GetLangId()}");
                        return;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
