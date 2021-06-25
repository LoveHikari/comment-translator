using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// 有道翻译
    /// </summary>
    public class YoudaoFanyi
    {
        // https://www.cnblogs.com/lingdurebing/p/ldrb-java-spider.html
        private readonly HttpClient _httpClient;
        public YoudaoFanyi()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
            };
            _httpClient = new HttpClient(handler);
            
        }
        public async Task<ApiResponse> Fanyi(string body, string from, string to, LanguageEnum fromLanguage, LanguageEnum toLanguage)
        {
            string ua = " Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0";
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://fanyi.youdao.com"));
            request.Headers.Add("User-Agent", ua);
            request.Headers.Add("Referer", "http://fanyi.youdao.com/");
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            string url = "https://fanyi.youdao.com/translate_o?smartresult=dict&smartresult=rule";

            string r = "";
            request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            var v = GetSign(body);
            var bv = GetBv(ua);
            IDictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("i", body);
            dic.Add("from", from);
            dic.Add("to", to);
            dic.Add("smartresult", "dict");
            dic.Add("client", "fanyideskweb");
            dic.Add("salt", v.salt);
            dic.Add("sign", v.sign);
            dic.Add("bv", bv);
            dic.Add("lts", v.ts);
            dic.Add("doctype", "json");
            dic.Add("version", "2.1");
            dic.Add("keyfrom", "fanyi.web");
            dic.Add("action", "FY_BY_REALTlME");
            var data = new FormUrlEncodedContent(dic);
            request.Content = data;
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
            request.Headers.Add("User-Agent", ua);
            request.Headers.Add("Referer", "http://fanyi.youdao.com/");
            response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                string html = System.Text.Encoding.UTF8.GetString(bytes);
                var jo = Newtonsoft.Json.Linq.JObject.Parse(html);
                var jr = Newtonsoft.Json.Linq.JArray.Parse(jo["translateResult"].ToString());
                r = JObject.Parse(jr[0][0].ToString())["tgt"].ToString();
            }

            var apiResp = new ApiResponse()
            {
                Code = (int)response.StatusCode,
                Message = response.StatusCode.ToString(),
                Data = r,
                FromLanguage = fromLanguage.ToString(),
                ToLanguage =toLanguage.ToString(),
                TranslateSuccess = true
            };

            return apiResp;
        }

        private (string ts, string salt, string sign) GetSign(string word)
        {
            var ts = GetTimeStamp();
            var salt = ts + new Random().Next(10);
            var sign = GetMd5("fanyideskweb" + word + salt + "n%A-rKaT5fb[Gy?;N5@Tj");
            return (ts, salt, sign);
        }

        private String GetBv(String UserAgent)
        {
            return GetMd5(UserAgent);
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        private string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data">要加密的数据</param>
        /// <returns>密文</returns>
        private string GetMd5(string data)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] t = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            string t2 = BitConverter.ToString(t);
            t2 = t2.Replace("-", "").ToLower();
            return t2;
        }
    }
}
