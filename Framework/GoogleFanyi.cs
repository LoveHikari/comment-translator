using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public class GoogleFanyi
    {
        public async Task<ApiResponse> Fanyi(string body, string from, string to, LanguageEnum fromLanguage, LanguageEnum toLanguage)
        {
            var client = new HttpClient();
            string r = "";
            string url = "https://translate.google.com/_/TranslateWebserverUi/data/batchexecute";
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            IDictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("f.req", $"[[[\"MkEWBc\",\"[[\\\"{body}\\\",\\\"{from}\\\",\\\"{to}\\\",true],[null]]\", null, \"generic\"]]]");
            var data = new FormUrlEncodedContent(dic);
            request.Content = data;
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                string html = System.Text.Encoding.UTF8.GetString(bytes);
                html = html.Replace("\\n", "").Replace(")]}'", "");
                var jo = Newtonsoft.Json.Linq.JArray.Parse(html);
                jo = Newtonsoft.Json.Linq.JArray.Parse(jo[0][2].ToString());

                r = jo[1][0][0][5][0][0].ToString();
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
