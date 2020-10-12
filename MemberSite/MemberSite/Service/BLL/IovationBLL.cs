using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class IovationBLL
    {
        private readonly IovationService iovationService;
        private readonly IConfiguration configuration;
        private readonly CourierAdapter courierAdapter;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly Tools tools;
        private readonly Dictionary<int, string> iovationType;
        private readonly Dictionary<int, string> perCountry;
        private readonly Dictionary<int, string> perCurrency;

        public IovationBLL(IConfiguration configuration, IovationService iovationService, CourierAdapter courierAdapter, Tools tools, ServerInfoBLL serverInfoBLL)
        {
            this.iovationService = iovationService;
            this.configuration = configuration;
            this.courierAdapter = courierAdapter;
            this.serverInfoBLL = serverInfoBLL;
            this.tools = tools;
            iovationType = new Dictionary<int, string>()
            {
                {1, "registration"  },
                {2, "withdrawal" }
            };
            perCountry = new Dictionary<int, string>()
            {
              {2,"MY" },{4,"TH" },{13,"CS" },{15,"ID" },{51,"VN" }
            };
            perCurrency = new Dictionary<int, string>()
            {
              {2,"MYR" },{4,"THB" },{13,"RMB" },{15,"IDR" },{51,"VND" }
            };
        }
        public void SetIovation(int custId, string iovation, IovationType type)
        {
            string typeName;
            iovationType.TryGetValue((int)type, out typeName);
            iovationService.SetIovation(custId, iovation, typeName);
        }
        private bool IsSiteSupportIovation()
        {
            bool isSupportIov = false;

            var supportDic = configuration.GetSection("IovationSupport")?.Get<Dictionary<string, bool>>();

            if (supportDic != null)
            {
                var supportDicIgnoreCase = new Dictionary<string, bool>(supportDic, StringComparer.OrdinalIgnoreCase);
                var serverInfo = serverInfoBLL.GetServerInfo();
                if (supportDicIgnoreCase.ContainsKey(serverInfo.SiteName))
                    supportDicIgnoreCase.TryGetValue(serverInfo.SiteName, out isSupportIov);
            }

            return isSupportIov;
        }
        public async Task<ApiResult> IovationChecks(int CustId, int CurrencyId, string UserName, string blackbox, IovationType type)
        {
            ApiResult apiResult = new ApiResult();
            if (!IsSiteSupportIovation())
            {
                apiResult.ErrorCode = (int)IovationErrorcode.IovationNotSupport;
                return apiResult;
            }

            string commonServiceUrl = configuration.GetValue<string>("CommonService");
            string apiUrl = $"{commonServiceUrl}Iovation/IovationChecks";
            string CountryName, Currencyname;
            perCountry.TryGetValue((int)CurrencyId, out CountryName);
            perCurrency.TryGetValue((int)CurrencyId, out Currencyname);
            Dictionary<string, string> transactionInsight = new Dictionary<string, string>();
            transactionInsight.Add("onlineId", UserName);
            transactionInsight.Add("billingCountry", CountryName);
            transactionInsight.Add("valueCurrency", Currencyname);
            string typeName;
            iovationType.TryGetValue((int)type, out typeName);
            dynamic req = new
            {
                accountCode = CustId,
                blackbox = blackbox,
                statedIp = tools.GetClientIP(),
                transactionInsight,
                type = typeName
            };

            apiResult = await courierAdapter.MyPostAsync<bool, dynamic>(apiUrl, req);

            return apiResult;
        }
    }
}
