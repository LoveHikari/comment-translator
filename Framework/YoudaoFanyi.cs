using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// 有道翻译
    /// </summary>
    /// <remarks>https://www.cnblogs.com/wxd501/p/17070184.html</remarks>
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
            var localtime = GetTimeStamp();
            var signData = $"client=fanyideskweb&mysticTime={localtime}&product=webfanyi&key=fsdsogkndfokasodnaso";
            var sign = System.BitConverter.ToString(MD5.Create().ComputeHash(System.Text.Encoding.GetEncoding("utf-8").GetBytes(signData))).Replace("-", "").ToLower();
            IDictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("i", body);
            dic.Add("from", from);
            dic.Add("to", to);
            dic.Add("dictResult", "true");
            dic.Add("keyid", "webfanyi");
            dic.Add("sign", sign);
            dic.Add("client", "fanyideskweb");
            dic.Add("product", "webfanyi");
            dic.Add("appVersion", "1.0.0");
            dic.Add("vendor", "web");
            dic.Add("pointParam", "client,mysticTime,product");
            dic.Add("mysticTime", localtime);
            dic.Add("keyfrom", "fanyi.web");
            var data = new FormUrlEncodedContent(dic);

            string ua = " Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0";
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://dict.youdao.com/webtranslate"));

            request.Content = data;
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
            request.Headers.Add("User-Agent", ua);
            request.Headers.Add("Referer", "http://fanyi.youdao.com/");
            request.Headers.Add("Cookie", "OUTFOX_SEARCH_USER_ID=-2094880112@10.108.162.135; OUTFOX_SEARCH_USER_ID_NCOO=86107500.53660281");
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string r = "";
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                string html = System.Text.Encoding.UTF8.GetString(bytes);
                html = DecryptedText(html);
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

        private string DecryptedText(string text)
        {
            string o = "ydsecret://query/key/B*RGygVywfNBwpmBaZg*WT7SIOUP2T0C9WHMZN39j^DAdaZhAnxvGcCY6VYFwnHl";
            string n = "ydsecret://query/iv/C@lZe2YzHtZ2CYgaXKSVfsb7Y4QWHjITPPZ0nQp87fBeJ!Iv6v^6fvi2WN@bYpJ4";
            var a = MD5.Create().ComputeHash(System.Text.Encoding.GetEncoding("utf-8").GetBytes(o));
            var r = MD5.Create().ComputeHash(System.Text.Encoding.GetEncoding("utf-8").GetBytes(n));

            text = text.Replace("-", "+").Replace("_", "/");

            Aes aes = Aes.Create();
            aes.KeySize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = a;
            aes.IV = r;

            ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] encryptedBytes = Convert.FromBase64String(text);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            string decryptedText = Encoding.UTF8.GetString(decryptedBytes);
            return decryptedText;
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
    }
}
