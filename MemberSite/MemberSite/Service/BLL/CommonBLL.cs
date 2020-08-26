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
        private readonly CustInfoService custInfoService;
        private readonly string commonServiceUrl;
        public CommonBLL(IConfiguration configuration, CourierAdapter courierAdapter, CustInfoService custInfoService)
        {
            this.courierAdapter = courierAdapter;
            this.custInfoService = custInfoService;
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
            string apiUrl = $"{commonServiceUrl}MoviderSMS/SendVerifySMS";

            dynamic req = new
            {
                CustId = custId,
            };

            ApiResult result = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return result;
        }
        public async Task<ApiResult> AcknowledgeVerifySMS(int custId, string siteName, string request_id, string code)
        {
            string apiUrl = $"{commonServiceUrl}MoviderSMS/AcknowledgeVerifySMS";

            dynamic req = new
            {
                siteName = siteName,
                request_id = request_id,
                code = code
            };

            ApiResult apiResult = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            if (apiResult.ErrorCode == 0)
            {
                custInfoService.SetPhoneVerified(custId);
            }

            return apiResult;
        }
    }
}
