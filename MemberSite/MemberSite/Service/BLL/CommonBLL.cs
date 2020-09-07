using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class CommonBLL
    {
        private readonly CourierAdapter courierAdapter;
        private readonly CustInfoBLL custInfoBLL;
        private readonly string commonServiceUrl;
        public CommonBLL(IConfiguration configuration, CourierAdapter courierAdapter, CustInfoBLL custInfoBLL)
        {
            this.courierAdapter = courierAdapter;
            this.custInfoBLL = custInfoBLL;
            commonServiceUrl = configuration.GetValue<string>("CommonService");
        }

        public void SendWelcomeMail(int custId)
        {
            string apiUrl = $"{commonServiceUrl}Mail/SendWelcomeMail";

            dynamic req = new
            {
                CustId = custId,
            };

            courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
        }

        public void SendResetPwdMail(int custId, string resetPasswordUrl)
        {
            string apiUrl = $"{commonServiceUrl}Mail/RestPassword";

            dynamic req = new
            {
                CustId = custId,
                resetPasswordUrl
            };

            courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);
        }
        public async Task<ApiResult> SendVerifySMSAsync(int custId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            string apiUrl = $"{commonServiceUrl}MoviderSMS/SendVerifySMS";

            dynamic req = new
            {
                CustId = custId,
            };

            if (custInfo.CurrencyId == 51)
            {
                apiUrl = $"{commonServiceUrl}TwilioSMS/VerificationsSMS";
            }

            ApiResult result = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return result;
        }
        public async Task<ApiResult> AcknowledgeVerifySMS(int custId, string siteName, string request_id, string code)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            string apiUrl = $"{commonServiceUrl}MoviderSMS/AcknowledgeVerifySMS";

            dynamic req = new
            {
                siteName = siteName,
                request_id = request_id,
                code = code
            };

            if (custInfo.CurrencyId == 51)
            {
                apiUrl = $"{commonServiceUrl}TwilioSMS/VerificationCheckSMS";

                req = new
                {
                    siteName = siteName,
                    sid = request_id,
                    code = code
                };
            }

            ApiResult apiResult = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            if (apiResult.ErrorCode == 0)
            {
                custInfoBLL.SetPhoneVerified(custId);
            }

            return apiResult;
        }
    }
}
