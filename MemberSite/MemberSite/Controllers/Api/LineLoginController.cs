using MemberSite.Service.Model.Line;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MemberSite.Controllers.Api
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class LineLoginController : Controller
    {
        private readonly string _channelId;
        private readonly string _channelSecret;
        private readonly string _sitePassword;
        private readonly string _redirectUri;

        #region Constructor

        public LineLoginController(IServiceProvider provider)
        {
            _channelId = "1656486984"; //之後改成用config設定
            _channelSecret = "0acdbf41d3fdc94fa7185e812100f8a4"; //之後改成用config設定
            _sitePassword = "Nova88Test"; //之後改成用config設定
            _redirectUri = "http://localhost:5000/api/LineLogin/callback";//之後改成用config設定
        }

        #endregion

        #region Action

        public ActionResult LineLoginDirect()
        {
            #region API Example & Description

            //Reference=https://developers.line.biz/zh-hant/docs/line-login/integrate-line-login/#making-an-authorization-request
            //Example=https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id=1234567890&redirect_uri=https%3A%2F%2Fexample.com%2Fauth&state=12345abcde&scope=profile%20openid&nonce=09876xyz

            //Url= https://access.line.me/oauth2/v2.1/authorize

            //Parameter
            //response_type          (String,必須)：code
            //client_id              (String, 必須)：LINE Login Channel 的 Channel ID。您可以在LINE Developers console中查看。
            //redirect_uri           (String, 必須)：在 LINE Developers Console 中註冊的 Callback URL。
            //state                  (String, 必須)：用於防止跨站請求偽造(opens new window)的唯一性屬性。 請在 Web app 請求中，針對每個登入階段隨機生成。注意，不能使用 URL 編碼過後的文字。
            //scope                  (String, 必須)：向用戶請求授予權限。詳細內容請參閱「Scopes」。
            //nonce                  (String, 選填)：用於防止Replay attack(opens new window)的文字。此值會在 Response 的 ID Token中。
            //prompt                 (String, 選填)：consent。即使已授予用戶所有請求的權限，仍然強制顯示同意畫面。
            //max_age                (Number, 選填)：用戶認證後，允許的最長經過時間（以秒為單位）。相當於在OpenID Connect Core 1.0(opens new window)的「Authentication Request」區段中，所定義的max_age屬性。
            //ui_locales             (String, 選填)：在 LINE Login 中，所顯示畫面的顯示語言和文字類型。按照優先度從高到低的順序，在用空格分隔的列表中，設定在RFC 5646（BCP 47） (opens new window)中定義的語言標籤。相當於在OpenID Connect Core 1.0(opens new window)的「Authentication Request」區段中，所定義的ui_locales屬性。
            //bot_prompt             (String, 選填)：用戶登入時，會顯示將 LINE 官方帳號加入好友的選項。指定normal或aggressive。詳細內容請參閱「在登入時將 LINE 官方帳號加為好友(bot link)」。
            //initial_amr_display    (String, 選填)：若設定為 lineqr 將會顯示 QR code 登入 來取代預設的 email 登入。
            //switch_amr             (Boolean, 選填)：預設值為 true。若設定為 false，則會隱藏用於更改登錄方式的按鈕，例如 "透過 Email 登入" 或 " QR Code 登入" 的選項。
            //disable_ios_auto_login (Boolean, 選填)：預設為 false，若設定為 true，將會停止在 iOS 上的自動登入機制。

            #endregion

            string response_type = "code";
            string client_id = _channelId;
            string redirect_uri = HttpUtility.UrlEncode(_redirectUri);
            string state = _sitePassword;
            string LineLoginUrl = string.Format("https://access.line.me/oauth2/v2.1/authorize?"+
                "response_type={0}"
                +"&client_id={1}"
                +"&redirect_uri={2}"
                +"&state={3}"
                +"&scope=profile%20openid%20email"
                //+ "&scope=openid%20profile%20real_name%20gender%20birthdate%20phone%20address%20email" //公司要申請Profile+才可以使用
                + "&nonce=1q2w3e4r",
                response_type,
                client_id,
                redirect_uri,
                state
            );
            return Redirect(LineLoginUrl);
        }

        public ActionResult callback(string code, string state, string error, string error_description)
        {
            if(string.IsNullOrEmpty(code))
            {
                string a = "aaa";
                //用戶在Line驗證畫面選擇取消，重新導頁回homepage
                //string a = VirtualPathUtility.ToAbsolute("~/");
            }

            if (state == _sitePassword)
            {
                #region Declare 

                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                string result = string.Empty;
                NameValueCollection nvc = new NameValueCollection();

                #endregion

                try
                {
                    #region OAuth (Get Access Token)

                    string ApiUrl_Token = "https://api.line.me/oauth2/v2.1/token";
                    nvc.Add("grant_type", "authorization_code");
                    nvc.Add("code", code);
                    nvc.Add("redirect_uri", _redirectUri);
                    nvc.Add("client_id", _channelId);
                    nvc.Add("client_secret", _channelSecret);
                    string JsonStr = Encoding.UTF8.GetString(wc.UploadValues(ApiUrl_Token, "POST", nvc));
                    IssueAccessTokenResponseModel ToKenObj = JsonConvert.DeserializeObject<IssueAccessTokenResponseModel>(JsonStr);
                    wc.Headers.Clear();

                    #endregion

                    //取回User Profile
                    //string ApiUrl_Profile = "https://api.line.me/v2/profile";
                    //wc.Headers.Add("Authorization", "Bearer " + ToKenObj.access_token);
                    //string UserProfile = wc.DownloadString(ApiUrl_Profile);
                    //LineProfile ProfileObj = JsonConvert.DeserializeObject<LineProfile>(UserProfile);
                    
                    //取email
                    var JwtSecurityToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(ToKenObj.id_token);
                    var email = "";
                    //如果有email
                    if (JwtSecurityToken.Claims.ToList().Find(c => c.Type == "email") != null)
                        email = JwtSecurityToken.Claims.First(c => c.Type == "email").Value;

                    return Json(JwtSecurityToken);
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    throw;
                }
            }
            return View();
        }

        #endregion
    }
}
