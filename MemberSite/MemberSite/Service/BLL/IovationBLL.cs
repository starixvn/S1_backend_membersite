using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Mapping;

namespace MemberSite.Service.BLL
{
    public class IovationBLL
    {
        private readonly IovationService iovationService;
        private readonly IConfiguration configuration;
        private readonly CourierAdapter courierAdapter;
        private readonly ServerInfoBll serverInfoBLL;
        private readonly CustInfoBll custInfoBLL;
        private readonly Tools tools;
        private readonly Dictionary<int, string> iovationType;
        private readonly Dictionary<int, string> perCountry;
        private readonly Dictionary<int, string> perCurrency;

        public IovationBLL(IConfiguration configuration, IovationService iovationService, CourierAdapter courierAdapter, Tools tools, ServerInfoBll serverInfoBLL, CustInfoBll custInfoBLL)
        {
            this.iovationService = iovationService;
            this.configuration = configuration;
            this.courierAdapter = courierAdapter;
            this.serverInfoBLL = serverInfoBLL;
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
            iovationType = new Dictionary<int, string>()
            {
                {1, "registration"  },
                {2, "withdrawal" }
            };
            perCountry = new CurrencyMapping().CountryMap;
            perCurrency = new Dictionary<int, string>()
            {
              {2,"MYR" },{3,"US" },{4,"THB" },{13,"CNY" },{15,"IDR" },{61,"INR" },{51,"VND" },{74,"BD" },
            };
        }
        public void SetIovation(int custId, IovationType type, IovationCheckResult iovation)
        {
            iovationService.SetIovation(custId, iovation.Result, type.ToString(), iovation.DeviceId, iovation.Reason,
                iovation.DeviceType, iovation.Score);
        }
        public async Task<ApiResult<IovationCheckResult>> IovationChecks(int CustId, int CurrencyId, string UserName, string blackbox, IovationType type)
        {
            var apiResult = new ApiResult<IovationCheckResult>();
            if (string.IsNullOrEmpty(blackbox))
            {
                apiResult.ErrorCode = (int)IovationErrorcode.IovationNotSupport;
                return apiResult;
            }

            var commonServiceUrl = configuration.GetValue<string>("CommonService");
            var apiUrl = $"{commonServiceUrl}Iovation/IovationChecks";
            string CountryName = "", Currencyname = "";
            if (CurrencyId == 96)
            {
                var custInfo = custInfoBLL.GetCustInfoProfileWithCache(CustId);
                if (custInfo != null)
                    CountryName = custInfoBLL.GetCountryNameByAreaCode(custInfo.AreaCode);
            }
            else
            {
                perCountry.TryGetValue((int)CurrencyId, out CountryName);
            }
            
            perCurrency.TryGetValue((int)CurrencyId, out Currencyname);
            var transactionInsight = new Dictionary<string, string>
            {
                { "onlineId", UserName },
                { "billingCountry", CountryName },
                { "valueCurrency", Currencyname }
            };
            iovationType.TryGetValue((int)type, out var typeName);
            dynamic req = new
            {
                accountCode = CustId,
                blackbox,
                statedIp = tools.GetClientIP(),
                transactionInsight,
                type = typeName
            };

            apiResult = await courierAdapter.MyPostAsync<IovationCheckResult, dynamic>(apiUrl, req);

            return apiResult;
        }
    }
}
