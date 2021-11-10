using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class LineBLL
    {
        private readonly string _channelId;
        private readonly string _channelSecret;
        private readonly string _sitePassword;
        private readonly string _redirectUri;

        #region Constructor

        public LineBLL(IServiceProvider provider)
        {
            _channelId = "1656486984"; //之後改成用config設定
            _channelSecret = "0acdbf41d3fdc94fa7185e812100f8a4"; //之後改成用config設定
            _sitePassword = "Nova88Test"; //之後改成用config設定
            _redirectUri = "http://localhost:5000/api/LineLogin/callback";//之後改成用config設定
        }

        #endregion

        #region Method

        

        #endregion

    }
}
