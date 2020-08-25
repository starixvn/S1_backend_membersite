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
            string apiUrl = $"{commonServiceUrl}MoviderSMS/SendVerifySMS";

            dynamic req = new
            {
                CustId = custId,
            };

            ApiResult<ApiResult> result = await courierAdapter.MyPostAsync<ApiResult, dynamic>(apiUrl, req);

            return result.Data;
        }
        public async Task<ApiResult> AcknowledgeVerifySMS(int custId, string request_id, string code)
        {
            string apiUrl = $"{commonServiceUrl}MoviderSMS/AcknowledgeVerifySMS";

            dynamic req = new
            {
                request_id = request_id,
                code = code
            };

            ApiResult<ApiResult> result = await courierAdapter.MyPostAsync<ApiResult, dynamic>(apiUrl, req);
            var apiResult = result.Data;

            if (apiResult.ErrorCode == 0)
            {
                custInfoBLL.SetPhoneVerified(custId);
            }

            return apiResult;
        }
    }
}
