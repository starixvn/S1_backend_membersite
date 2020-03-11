using MemberSite.Service.Library;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class GamingBLL
    {
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;

        public GamingBLL(Tools tools, SelLangBLL selLangBLL)
        {
            this.tools = tools;
            this.selLangBLL = selLangBLL;
        }
        public string GetEntPageUrl(int tpId, string gameId, IUrlHelper urlHelper)
        {
            var entPageUrl = "";
            if (tpId == 1)
            {
                if (!urlHelper.ActionContext.HttpContext.User.Identity.IsAuthenticated)
                {
                    if (gameId == "202" || gameId == "208" || gameId == "211")
                    {
                        if (tools.IsMobile())
                        {
                            var message = selLangBLL.GetMsg("msg_LoginToAccess");
                            entPageUrl = $"javascript:alert('{message}');";
                        }
                        else if (gameId == "211")
                        {
                            entPageUrl = urlHelper.RouteUrl("desktop", new { action = "Index", controller = "LiveCasino", LangId = selLangBLL.GetLangId() });
                        }
                        else
                        {
                            entPageUrl = urlHelper.RouteUrl("desktop", new { action = "Static", controller = "Home", LangId = selLangBLL.GetLangId(), tpId = tpId, gameId = gameId });
                        }
                    }
                    else
                    {
                        entPageUrl = urlHelper.RouteUrl("desktop", new { action = "Index", controller = "sports", LangId = selLangBLL.GetLangId(), tpId = tpId, gameId = gameId });
                    }
                }
                else
                {
                    var routeData = urlHelper.ActionContext.RouteData;
                    var homeUrl = urlHelper.RouteUrl("desktop", new { action = routeData.Values["action"], controller = routeData.Values["controller"], LangId = selLangBLL.GetLangId() }, protocol: urlHelper.ActionContext.HttpContext.Request.Scheme, host: urlHelper.ActionContext.HttpContext.Request.Host.Host);
                    var signupUrl = urlHelper.RouteUrl("desktop", new { action = "Register", controller = "Home", LangId = selLangBLL.GetLangId() }, protocol: urlHelper.ActionContext.HttpContext.Request.Scheme, host: urlHelper.ActionContext.HttpContext.Request.Host.Host);
                    var LoginUrl = urlHelper.RouteUrl("desktop", new { action = routeData.Values["action"], controller = routeData.Values["controller"], LangId = selLangBLL.GetLangId(), loginCont = 1 }, protocol: urlHelper.ActionContext.HttpContext.Request.Scheme, host: urlHelper.ActionContext.HttpContext.Request.Host.Host);

                    if (gameId == "202" || gameId == "208")/*Keno*/
                    {
                        entPageUrl = urlHelper.RouteUrl("desktop", new { action = "Index", controller = "entPage", LangId = selLangBLL.GetLangId(), homeUrl = homeUrl, signupUrl = signupUrl, LoginUrl = LoginUrl, tpId = tpId, gameId = gameId });
                        //entPageUrl = $"javascript:window.open('{entPageUrl}', 'Tp_{tpId}_{gameId}', 'toolbar=no, menubar=no,scrollbars=no,resizable=no,location=no,directories=no,status=no').focus();";
                        entPageUrl = $"javascript:LaunchGame('{entPageUrl}', 'Tp_{tpId}_{gameId}',{tpId}, {gameId});";
                    }
                    else if (gameId == "211")/*AllBet*/
                    {
                        entPageUrl = urlHelper.RouteUrl("desktop", new { action = "Index", controller = "LiveCasino", LangId = selLangBLL.GetLangId() });
                    }
                    else if (tools.IsMobile())
                    {
                        entPageUrl = urlHelper.RouteUrl("desktop", new { action = "Index", controller = "entPage", LangId = selLangBLL.GetLangId(), homeUrl = homeUrl, signupUrl = signupUrl, LoginUrl = LoginUrl, tpId = tpId, gameId = gameId });
                        entPageUrl = $"javascript:LaunchGame('{entPageUrl}', 'Tp_{tpId}_{gameId}',{tpId}, {gameId});";
                    }
                    else
                    {
                        entPageUrl = urlHelper.RouteUrl("desktop", new { action = "Index", controller = "sports", LangId = selLangBLL.GetLangId(), tpId = tpId, gameId = gameId });
                        entPageUrl = $"javascript:LaunchGame('{entPageUrl}', 'Tp_{tpId}_{gameId}',{tpId}, {gameId});";
                    }
                }
            }
            else
            {
                entPageUrl = urlHelper.RouteUrl("desktop", new { action = "Index", controller = "LiveCasino", LangId = selLangBLL.GetLangId() });
            }
            return entPageUrl;
        }
    }
}
