using System.Text;
using MemberSite.Service.BLL;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using static MemberSite.Extensions.MethodExtension;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class QuestionnaireController : Controller
    {
        private readonly ILogger<QuestionnaireController> _logger;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly QuestionnaireBLL _questionnaireBll;
        private readonly CustInfoBll _custInfoBll;

        public QuestionnaireController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<QuestionnaireController>>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _questionnaireBll = provider.GetRequiredService<QuestionnaireBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        }

        [HttpPost]
        public async Task<ApiResult> Set(Questionnairerequest request)
        {
            var apiResult = new ApiResult();

            try
            {
                var authData = _authorizeBll.GetAuthData();
                var custInfo = _custInfoBll.GetCustInfoWithCache(authData.CustId);
                if (authData is not { CustId: > 0 })
                {
                    apiResult.Message = "not login";
                    return apiResult;
                }

                var result = _questionnaireBll.Set(request, custInfo);
                if (result == 1)
                {
                    apiResult.Message = "Success";
                    apiResult.ErrorCode = 0; 
                }
                else 
                {
                    apiResult.Message = "You have reached the maximum feedback submissions for today.";
                    apiResult.ErrorCode = 9000; 
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                apiResult.Message = e.Message;
                apiResult.ErrorCode = 9000; 
            }

            return apiResult;
        }
    }
}