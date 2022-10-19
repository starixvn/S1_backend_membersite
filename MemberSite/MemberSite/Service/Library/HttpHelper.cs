using System.Net;
using System.Xml.Linq;
using Flurl.Http;
using Flurl.Http.Content;
using Newtonsoft.Json;

namespace MemberSite.Service.Library
{
    public sealed class HttpHelper : IHttpHelper
    {
        private readonly ILogger<HttpHelper> _logger;
        private string _authToken = string.Empty;
        private readonly Dictionary<string, string> _header;
        private readonly JsonNetSerializer _serializer;

        public HttpHelper(ILogger<HttpHelper> logger, JsonNetSerializer serializer)
        {
            _logger = logger;
            _serializer = serializer;
            _header = new Dictionary<string, string>();
        }

        public async Task<(TResult Result, HttpStatusCode StatusCode)> GetAsync<TResult>(string url, string query = null)
            where TResult : class
        {
            try
            {
                var tResult = default(TResult);
                var result = await GetDataAsync(url, query);
                _header.Clear();
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    tResult = _serializer.DeserializeFormStream<TResult>(result.ContentStream);
                }

                return (tResult, result.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ex massage: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public (TResult Result, HttpStatusCode StatusCode) Post<TBody, TResult>(string url, TBody body)
            where TResult : class
        {
            try
            {
                var returnObj = PostData(url, body, "string");
                var result = JsonConvert.DeserializeObject<TResult>(returnObj.Result);

                return (result, returnObj.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ex massage: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }
        public (TResult Result, HttpStatusCode StatusCode) FormPost<TBody, TResult>(string url, TBody body)
            where TResult : class
        {
            try
            {
                var returnObj = PostData(url, body, "x-www-form-urlencoded");
                var result = JsonConvert.DeserializeObject<TResult>(returnObj.Result);

                return (result, returnObj.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ex massage: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public (TResult Result, HttpStatusCode StatusCode) JsonPost<TBody, TResult>(string url, TBody data)
            where TResult : class
        {
            try
            {
                var returnObj = PostData(url, data, "json");
                var result = JsonConvert.DeserializeObject<TResult>(returnObj.Result);
                return (result, returnObj.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ex massage: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
            
        }

        public IHttpHelper WithHeader(string key, string value)
        {
            if (!_header.ContainsKey(key))
            {
                _header.Add(key, value);
            }

            if (_header.ContainsKey(key) && _header[key] != value)
            {
                _header[key] = value;
            }

            return this;
        }

        private FlurlRequest CreateFlurlRequest(string url)
        {
            var request = new FlurlRequest(url);
            if (_authToken != string.Empty)
            {
                request.WithOAuthBearerToken(_authToken);
            }

            return request;
        }

        private (string Result, HttpStatusCode StatusCode) PostData<TBody>(string url, TBody data, string postType)
        {
            var request = CreateFlurlRequest(url);
            request = _header.Aggregate(request, (current, item) => current.WithHeader(item.Key, item.Value));

            var response = default(HttpResponseMessage);
            if (postType == "json")
            {
                response = request.PostJsonAsync(data).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else if (postType == "string")
            {
                var content = data.ToString();
                response = request.PostStringAsync(content).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else if (postType == "x-www-form-urlencoded")
            {
                var content = data.ToString();
                response = request.PostUrlEncodedAsync(content).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            var result = response.Content.ReadAsStringAsync().Result;
            _header.Clear();
            return (Result: result, response.StatusCode);
        }

        private async Task<(Stream ContentStream, HttpStatusCode StatusCode)> GetDataAsync(string url, string query)
        {
            var urlWithQuery = url;
            if (string.IsNullOrEmpty(query))
            {
                urlWithQuery = $"{url}?{query}";
            }
            
            var request = CreateFlurlRequest(urlWithQuery);
            request = _header.Aggregate(request, (current, item) => current.WithHeader(item.Key, item.Value));

            _logger.LogInformation(
                $"header, Keys: {string.Join(",", request.Headers.Keys.ToList())}, Values: {string.Join(",", request.Headers.Values.ToList())}");
            var response = await request.GetAsync();
            _header.Clear();
            var stream = response.Content.ReadAsStreamAsync();

            return (await stream, response.StatusCode);
        }
    }
}