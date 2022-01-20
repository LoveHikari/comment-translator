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
            apiRequest.Body = apiRequest.Body.Replace("\r\n", "\n");
            apiRequest.Body = HumpUnfold(apiRequest.Body);
            string fromLanguage = LanguageTransform(apiRequest.TranslateServer, apiRequest.FromLanguage);
            string toLanguage = LanguageTransform(apiRequest.TranslateServer, apiRequest.ToLanguage);

            switch (apiRequest.TranslateServer)
            {
                case TranslateServerEnum.Google:
                    GoogleFanyi googleFanyi = new GoogleFanyi();
                    return await googleFanyi.Fanyi(apiRequest.Body, fromLanguage, toLanguage, apiRequest.FromLanguage, apiRequest.ToLanguage);
                case TranslateServerEnum.Bing:
                    BingFanyi bingFanyi = new BingFanyi();
                    return await bingFanyi.Fanyi(apiRequest.Body, fromLanguage, toLanguage, apiRequest.FromLanguage, apiRequest.ToLanguage);
                case TranslateServerEnum.百度:
                    break;
                case TranslateServerEnum.有道:
                    YoudaoFanyi youdaoFanyi = new YoudaoFanyi();
                    return await youdaoFanyi.Fanyi(apiRequest.Body, fromLanguage, toLanguage, apiRequest.FromLanguage, apiRequest.ToLanguage);
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
            string[] ss = humpString.Split(' ');
            string res = "";
            foreach (var s in ss)
            {
                Regex regex = new Regex("([A-Z]|^)[a-z]+");
                var matcher = regex.Matches(s);
                if (matcher.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (Match match in matcher)
                    {
                        string g = match.Groups[0].Value;
                        sb.Append(g + " ");
                    }

                    res += sb.ToString().TrimEnd() + " ";
                    //return sb.ToString().TrimEnd();
                }
                else
                {
                    res += s + " ";
                }
            }

            return res;
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
                    switch (language)
                    {
                        case LanguageEnum.Auto:
                            s = "auto";
                            break;
                        case LanguageEnum.日本語:
                            s = "ja";
                            break;
                        case LanguageEnum.简体中文:
                            s = "zh-CHS";
                            break;
                        case LanguageEnum.繁體中文:
                            s = "zh-CHS";
                            break;
                        case LanguageEnum.English:
                            s = "en";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(language), language, null);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(translateServer), translateServer, null);
            }

            return s;
        }
    }
}
