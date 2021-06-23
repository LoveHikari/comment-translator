using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Framework
{
    public class BingFanyi
    {
        public async Task<ApiResponse> Fanyi(string body, string from, string to, LanguageEnum fromLanguage, LanguageEnum toLanguage)
        {
            var client = new HttpClient();
            string r = "";
            string url = "https://cn.bing.com/translator";
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            HttpResponseMessage response = await client.SendAsync(request);
            string html = await response.Content.ReadAsStringAsync();
            Regex regex = new Regex("params_RichTranslateHelper = \\[(.+?),\"(.+?)\",3600000,true\\]");
            var match = regex.Match(html);
            string token = match.Groups[2].Value;
            string key = match.Groups[1].Value;

            url = "https://cn.bing.com/ttranslatev3";
            request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            IDictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("fromLang", from);
            dic.Add("text", body);
            dic.Add("to", to);
            dic.Add("token", token);
            dic.Add("key", key);
            var data = new FormUrlEncodedContent(dic);
            request.Content = data;
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
            response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                html = System.Text.Encoding.UTF8.GetString(bytes);
                var doc = Newtonsoft.Json.Linq.JArray.Parse(html);

                r = doc[0]["translations"][0]["text"].ToString();
            }

            var apiResp = new ApiResponse()
            {
                Code = (int)response.StatusCode,
                Message = response.StatusCode.ToString(),
                Data = r,
                FromLanguage = fromLanguage.ToString(),
                ToLanguage = toLanguage.ToString(),
                TranslateSuccess = true
            };

            return apiResp;
        }
    }
}
