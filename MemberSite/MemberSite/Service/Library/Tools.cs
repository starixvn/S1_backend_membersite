using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MemberSite.Service.Library
{
    public class Tools
    {
        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="pass"></param>
        /// <returns></returns>
        public string MD5Encrypt(string pass)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(pass));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
        /// <summary>
        /// 验证用户名 必须是字母和数字的组合
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool CheckUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName) || userName.Length < 6 || userName.Length > 18)
                return false;

            Regex regex = new Regex("^(?!^\\d+$)(?!^[a-zA-Z]+$)[0-9a-zA-Z]{6,18}$", RegexOptions.Singleline);
            Match mPwd = regex.Match(userName);

            return mPwd.Success;
        }
        /// <summary>
        /// 验证用户密码："^[a-zA-Z]\w{5,17}$"正确格式为：以字母开头，长度在6~18之间，只能包含字符、数字和下划线。
        /// </summary>
        /// <param name="userPwd"></param>
        /// <returns></returns>
        public bool CheckUserInputPassword(string userPwd)
        {
            if (string.IsNullOrEmpty(userPwd) || userPwd.Length < 6 || userPwd.Length > 18)
                return false;

            Regex regex = new Regex("^[A-Za-z0-9]+$", RegexOptions.Singleline);
            Match mPwd = regex.Match(userPwd);

            return mPwd.Success;
        }

        public string GetToken()
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");

            return GuidString;
        }
        #region GetClientIP
        /// <summary>
        /// 获取客户端IP
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public string GetClientIP(Microsoft.AspNetCore.Http.HttpContext context)
        {
            string[] HeaderNames = new string[] { "HTTP_X_FORWARDED_FOR", "HTTP_INCAP_CLIENT_IP", "HTTP_TRUE_CLIENT_IP", "HTTP_X_REAL_IP", "REMOTE_ADDR" };

            foreach (string Header in HeaderNames)
            {

                if (context.Request.Headers.ContainsKey(Header))
                {
                    string sServerVariables = context.Request.Headers[Header];
                    string ip = GetIPAddress(sServerVariables.Split(','));
                    if (!string.IsNullOrEmpty(ip))
                    {
                        return ip;
                    }
                }
            }

            return context.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

        private string GetIPAddress(string[] IPArry)
        {
            foreach (string ip in IPArry)
            {
                string IPAddress = ip.Trim();
                if (IsIPAddress(IPAddress)
                    && !isInnerIP(IPAddress)) return IPAddress.Trim();
            }

            return null;
        }
        private bool isInnerIP(string ipAddress)
        {
            bool isInnerIp = false;
            long ipNum = getIpNum(ipAddress);

            long aBegin = 167772160;// getIpNum("10.0.0.0");
            long aEnd = 184549375;// getIpNum("10.255.255.255");
            long bBegin = 2886729728; //getIpNum("172.16.0.0");
            long bEnd = 2887778303; //getIpNum("172.31.255.255");
            long cBegin = 3232235520; //getIpNum("192.168.0.0");
            long cEnd = 3232301055; //getIpNum("192.168.255.255");

            // Logger.Log("aBegin:" + aBegin + ",aEnd:" + aEnd + ",bBegin:" + bBegin + ",bEnd:" + bEnd + ",cBegin:" + cBegin + ",cEnd:" + cEnd);

            isInnerIp = isInner(ipNum, aBegin, aEnd) || isInner(ipNum, bBegin, bEnd) || isInner(ipNum, cBegin, cEnd) || ipAddress == "127.0.0.1";
            return isInnerIp;
        }
        private long getIpNum(String ipAddress)
        {
            string[] ip = ipAddress.Split('.');
            long a = Convert.ToInt32(ip[0]);
            long b = Convert.ToInt32(ip[1]);
            long c = Convert.ToInt32(ip[2]);
            long d = Convert.ToInt32(ip[3]);

            long ipNum = a * 256 * 256 * 256 + b * 256 * 256 + c * 256 + d;
            return ipNum;
        }
        private bool IsIPAddress(string str1)
        {
            if (string.IsNullOrEmpty(str1) || str1.Length < 7 || str1.Length > 15) return false;
            const string regformat = @"^\d{1,3}[\.]\d{1,3}[\.]\d{1,3}[\.]\d{1,3}$";

            Regex regex = new Regex(regformat, RegexOptions.IgnoreCase);
            return regex.IsMatch(str1);

        }
        private bool isInner(long userIp, long begin, long end)
        {
            return (userIp >= begin) && (userIp <= end);
        }
        #endregion
        public string GetFromUrl(Microsoft.AspNetCore.Http.HttpContext context)
        {
            string fromUrl = "";
            if (context.Request.Headers.ContainsKey("Referer"))
            {
                fromUrl = context.Request.Headers["Referer"].ToString();
            }

            return fromUrl;
        }
        /// <summary>
        /// 保留两位小数的金额
        /// </summary>
        /// <param name="d"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public decimal Floor(decimal d, int decimals)
        {
            long value = (long)Math.Pow(10.0, decimals);
            return Convert.ToDecimal(Math.Floor(d * (decimal)value) / (decimal)value);
        }
        /// <summary>
        /// 余额
        /// </summary>
        /// <param name="dBalance"></param>
        /// <returns></returns>
        public string FmtBalance(decimal dBalance)
        {
            return Floor(dBalance, 2).ToString("#,##0.#0");
        }
    }
}
