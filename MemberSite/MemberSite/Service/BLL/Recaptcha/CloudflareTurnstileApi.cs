using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL;

public class CloudflareTurnstileApi : IRecaptchaApi
{
    private readonly CourierAdapter _courierAdapter;
    private readonly ILogger<CloudflareTurnstileApi> _logger;
    public CloudflareTurnstileApi(IServiceProvider provider)
    {
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        _logger = provider.GetRequiredService<ILogger<CloudflareTurnstileApi>>();
    }

    public CaptchaCommonResponse Verify(string url, string secret, string token)
    {
        var values = new Dictionary<string, string>
        {
            { "secret", secret },
            { "response", token }
        };

        var content = new FormUrlEncodedContent(values);
        //var response = _courierAdapter.PostAsync<TurnstileResponse, FormUrlEncodedContent>(url, content).Result;
        //var responseString = response.Content.ReadAsStringAsync().Result;
        //var responseModel = JsonConvert.DeserializeObject<TurnstileResponse>(responseString);

        // _logger.LogInformation($"CloudflareTurnstile Response:{responseString}");

        return null;
        //return new CaptchaCommonResponse(responseModel);
    }

}
