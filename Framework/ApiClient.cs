using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Framework
{
    public class ApiClient
    {
        public async Task<ApiResponse> Execute(ApiRequest apiRequest)
        {
            apiRequest.Body = apiRequest.Body.Replace("\r\n", @"\\n");
            apiRequest.Body = HumpUnfold(apiRequest.Body);
            string fromLanguage = LanguageTransform(apiRequest.TranslateServer, apiRequest.FromLanguage);
            string toLanguage = LanguageTransform(apiRequest.TranslateServer, apiRequest.ToLanguage);

            switch (apiRequest.TranslateServer)
            {
                case TranslateServerEnum.Google:
                    return await Google(apiRequest, fromLanguage, toLanguage);
                case TranslateServerEnum.Bing:
                    return await Bing(apiRequest, fromLanguage, toLanguage);
                case TranslateServerEnum.百度:
                    break;
                case TranslateServerEnum.有道:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }

        /// <summary>
        ///  驼峰展开
        /// </summary>
        /// <param name="humpString"></param>
        /// <returns></returns>
        private String HumpUnfold(String humpString)
        {
            Regex regex = new Regex("([A-Z]|^)[a-z]+");
            var matcher = regex.Matches(humpString);
            if (matcher.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Match match in matcher)
                {
                    string g = match.Groups[0].Value;
                    sb.Append(g + " ");
                }

                return sb.ToString().TrimEnd();
            }

            return humpString;
        }

        private async Task<ApiResponse> Google(ApiRequest apiRequest, string fromLanguage, string toLanguage)
        {
            
            var client = new HttpClient();
            string r = "";
            string url = "https://translate.google.cn/_/TranslateWebserverUi/data/batchexecute";
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            IDictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("f.req", $"[[[\"MkEWBc\",\"[[\\\"{apiRequest.Body}\\\",\\\"{fromLanguage}\\\",\\\"{toLanguage}\\\",true],[null]]\", null, \"generic\"]]]");
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

                r = jo[1][0][0][5][0][0].ToString(); ;
            }

            var apiResp = new ApiResponse()
            {
                Code = (int)response.StatusCode,
                Message = response.StatusCode.ToString(),
                Data = r,
                FromLanguage = apiRequest.FromLanguage.ToString(),
                ToLanguage = apiRequest.ToLanguage.ToString(),
                TranslateSuccess = true
            };

            return apiResp;
        }
        private async Task<ApiResponse> Bing(ApiRequest apiRequest, string fromLanguage, string toLanguage)
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
            dic.Add("fromLang", fromLanguage);
            dic.Add("text", apiRequest.Body);
            dic.Add("to", toLanguage);
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
                FromLanguage = apiRequest.FromLanguage.ToString(),
                ToLanguage = apiRequest.ToLanguage.ToString(),
                TranslateSuccess = true
            };

            return apiResp;
        }

        private string LanguageTransform(TranslateServerEnum translateServer, LanguageEnum language)
        {
            string s = "";
            switch (translateServer)
            {
                case TranslateServerEnum.Bing:
                    switch (language)
                    {
                        case LanguageEnum.Auto:
                            s = "auto-detect";
                            break;
                        case LanguageEnum.日本語:
                            s = "ja";
                            break;
                        case LanguageEnum.简体中文:
                            s = "zh-Hans";
                            break;
                        case LanguageEnum.繁體中文:
                            s = "zh-Hant";
                            break;
                        case LanguageEnum.English:
                            s = "en";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(language), language, null);
                    }
                    break;
                case TranslateServerEnum.Google:
                    switch (language)
                    {
                        case LanguageEnum.Auto:
                            s = "auto";
                            break;
                        case LanguageEnum.日本語:
                            s = "ja";
                            break;
                        case LanguageEnum.简体中文:
                            s = "zh-CN";
                            break;
                        case LanguageEnum.繁體中文:
                            s = "zh-TW";
                            break;
                        case LanguageEnum.English:
                            s = "en";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(language), language, null);
                    }
                    break;
                case TranslateServerEnum.百度:
                    break;
                case TranslateServerEnum.有道:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(translateServer), translateServer, null);
            }

            return s;
        }
    }
}
