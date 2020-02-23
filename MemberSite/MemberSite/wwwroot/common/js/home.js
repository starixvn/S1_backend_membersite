string GetEntPageUrl(int tpId, string gameId)
{
    var entPageUrl = "";
    if (tpId == 1) {
        if (!User.Identity.IsAuthenticated && (gameId == "202" || gameId == "208" || gameId == "211")) {
            if (tools.IsMobile()) {
                var message = selLangBLL.GetMsg("msg_LoginToAccess");
                entPageUrl = "javascript:alert('" + message + "');";
            }
            else
                entPageUrl = Url.RouteUrl("desktop", new { action = "Static", controller = "Home", LangId = selLangBLL.GetLangId(), tpId = tpId, gameId = gameId });
        }
        else {
            var homeUrl = Url.RouteUrl("desktop", new { action = ViewContext.RouteData.Values["action"], controller = ViewContext.RouteData.Values["controller"], LangId = selLangBLL.GetLangId() }, protocol: Context.Request.Scheme, host: Context.Request.Host.Host);
            var signupUrl = Url.RouteUrl("desktop", new { action = "Register", controller = "Home", LangId = selLangBLL.GetLangId() }, protocol: Context.Request.Scheme, host: Context.Request.Host.Host);
            var LoginUrl = Url.RouteUrl("desktop", new { action = ViewContext.RouteData.Values["action"], controller = ViewContext.RouteData.Values["controller"], LangId = selLangBLL.GetLangId(), loginCont = 1 }, protocol: Context.Request.Scheme, host: Context.Request.Host.Host);

            if (gameId == "202" || gameId == "208" || gameId == "211") {
                entPageUrl = Url.RouteUrl("desktop", new { action = "Index", controller = "entPage", LangId = selLangBLL.GetLangId(), homeUrl = homeUrl, signupUrl = signupUrl, LoginUrl = LoginUrl, tpId = tpId, gameId = gameId });
                entPageUrl = $"javascript:window.open('{entPageUrl}', 'Tp_{tpId}_{gameId}', 'width=1366, height=1024').focus();";
            }
            else if (tools.IsMobile()) {
                entPageUrl = Url.RouteUrl("desktop", new { action = "Index", controller = "entPage", LangId = selLangBLL.GetLangId(), homeUrl = homeUrl, signupUrl = signupUrl, LoginUrl = LoginUrl, tpId = tpId, gameId = gameId });
            }
            else {
                @* IFrame *@
                entPageUrl = Url.RouteUrl("desktop", new { action = "Index", controller = "sports", LangId = selLangBLL.GetLangId(), tpId = tpId, gameId = gameId });
            }
        }
    }
    return entPageUrl;
}