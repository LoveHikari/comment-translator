using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Framework
{
    public class ApiClient
    {
        public async Task<ApiResponse> Execute(ApiRequest apiRequest)
        {
            var client = new HttpClient();
            string r = "";
            HttpResponseMessage response = null;
            switch (apiRequest.TranslateServer)
            {
                case TranslateServerEnum.谷歌:
                    {
                        string url = "https://translate.google.cn/_/TranslateWebserverUi/data/batchexecute";
                        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
                        IDictionary<string, string> dic = new Dictionary<string, string>();
                        dic.Add("f.req", $"[[[\"MkEWBc\",\"[[\\\"{apiRequest.Body}\\\",\\\"{apiRequest.FromLanguage}\\\",\\\"{apiRequest.ToLanguage}\\\",true],[null]]\", null, \"generic\"]]]");
                        var data = new FormUrlEncodedContent(dic);
                        request.Content = data;
                        response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            var bytes = await response.Content.ReadAsByteArrayAsync();
                            string html = System.Text.Encoding.UTF8.GetString(bytes);
                            Regex regex = new Regex("\\\\\"(.+?)\\\\\"");
                            var ms = regex.Matches(html);

                            r = ms[2].Groups[1].Value;
                        }

                        break;
                    }
                case TranslateServerEnum.百度:
                    break;
                case TranslateServerEnum.有道:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var apiResp = new ApiResponse()
            {
                Code = (int)response.StatusCode,
                Message = response.StatusCode.ToString(),
                Data = r,
                FromLanguage = apiRequest.FromLanguage,
                ToLanguage = apiRequest.ToLanguage,
                TranslateSuccess = true
            };

            return apiResp;
        }
    }
}
