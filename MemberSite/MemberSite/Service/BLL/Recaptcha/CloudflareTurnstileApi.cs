using MemberSite.Service.BLL.Interface;
using MemberSite.Service.Model;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL;

public class CloudflareTurnstileApi : IRecaptchaApi
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudflareTurnstileApi> _logger;
    public CloudflareTurnstileApi(IServiceProvider provider, HttpClient httpClient)
    {
        _httpClient = httpClient;
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
        var response = _httpClient.PostAsync(url, content).Result;
        var responseString = response.Content.ReadAsStringAsync().Result;
        var responseModel = JsonConvert.DeserializeObject<TurnstileResponse>(responseString);

        _logger.LogInformation($"CloudflareTurnstile Response:{responseString}");

        return new CaptchaCommonResponse(responseModel);
    }

}
