using MemberSite.Service.Library;
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
        private readonly string commonServiceUrl;
        public CommonBLL(IConfiguration configuration, CourierAdapter courierAdapter)
        {
            this.courierAdapter = courierAdapter;
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
    }
}
