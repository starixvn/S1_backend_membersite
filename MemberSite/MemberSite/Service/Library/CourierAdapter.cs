using MemberSite.Service.Model;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using Serilog.Context;

namespace MemberSite.Service.Library
{
    public class CourierAdapter
    {
        private static readonly Lazy<HttpClient> lazy = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        });

        private HttpClient client => lazy.Value;
        private readonly ILogger<CourierAdapter> _logger;

        public CourierAdapter(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CourierAdapter>>();
        }

        public async Task<ApiResult<T>> MyPostAsync<T, U>(string url, U oTpInfo)
        {
            ApiResult<T> oApiResult;
            var fooJson = JsonConvert.SerializeObject(oTpInfo);
            LogContext.PushProperty("TpInfo", fooJson);
            LogContext.PushProperty("Tp.Request.URL", url);
            var fooContent = new StringContent(fooJson, Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync(url, fooContent);
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Tp.Request.Response:{result}");

                if (response.IsSuccessStatusCode)
                {
                    oApiResult = JsonConvert.DeserializeObject<ApiResult<T>>(result);
                }
                else
                {
                    oApiResult = new ApiResult<T>
                    {
                        ErrorCode = 9000,
                        RawMessage = $"{result} {url}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                oApiResult = new ApiResult<T>
                {
                    ErrorCode = 9000,
                    RawMessage = $"{ex.Message} {url}"
                };
            }
            return oApiResult;
        }
        
        public async Task<T> ShieldGetAsync<T, U>(string url, U header) where T : new()
        {
            T oApiResult = default;
            var headerJson = JsonConvert.SerializeObject(header);
            var jo = JObject.Parse(headerJson);
            
            LogContext.PushProperty("TpHeader", headerJson);
            LogContext.PushProperty("Tp.Request.URL", url);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Site-Id", jo["SiteId"]?.ToString());
            httpClient.DefaultRequestHeaders.Add("Timestamp", jo["Timestamp"]?.ToString());
            httpClient.DefaultRequestHeaders.Add("Shield-Signature", jo["ShieldSignature"]?.ToString());

            try
            {
                using var response = await httpClient.GetAsync(url);
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Tp.Request.Response:{result}");

                if (response.IsSuccessStatusCode)
                {
                    oApiResult = JsonConvert.DeserializeObject<T>(result);
                }
                else
                {
                    oApiResult = new T();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return oApiResult;
        }
        
        public async Task<T> ShieldPostAsync<T, U>(string url, U oTpInfo ,U header) where T : new()
        {
            T oApiResult = default;
            var fooJson = JsonConvert.SerializeObject(oTpInfo);
            var fooContent = new StringContent(fooJson, Encoding.UTF8, "application/json");
            
            LogContext.PushProperty("TpInfo", fooJson);
            LogContext.PushProperty("TpHeader", header);
            LogContext.PushProperty("Tp.Request.URL", url);
            
            var headerJson = JsonConvert.SerializeObject(header);
            var jo = JObject.Parse(headerJson);
   
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Site-Id", jo["SiteId"]?.ToString());
            httpClient.DefaultRequestHeaders.Add("Timestamp", jo["Timestamp"]?.ToString());
            httpClient.DefaultRequestHeaders.Add("Shield-Signature", jo["ShieldSignature"]?.ToString());
            httpClient.DefaultRequestHeaders.Add("X-Endpoint-Version", jo["XEndpointVersion"]?.ToString());
            
            try
            {
                using var response = await httpClient.PostAsync(url, fooContent);
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Tp.Request.Response:{result}");

                if (response.IsSuccessStatusCode)
                {
                    oApiResult = JsonConvert.DeserializeObject<T>(result);
                }
                else
                {
                    oApiResult = new T();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return oApiResult;
        }
    }
}
