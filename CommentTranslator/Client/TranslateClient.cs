using System.Threading.Tasks;
using CommentTranslator.Util;
using Framework;

namespace CommentTranslator.Client
{
    public class TranslateClient
    {
        private Settings _settings;

        public TranslateClient(Settings settings)
        {
            _settings = settings;
        }
        /// <summary>
        /// 翻译
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<ApiResponse> TranslateAsync(string text)
        {
            var request = new ApiRequest()
            {
                TranslateServer = _settings.TranslateServer,
                FromLanguage = _settings.TranslateFrom,
                ToLanguage = _settings.TranslatetTo,
                Body = text
            };
            ApiClient apiClient = new ApiClient();
            return await apiClient.Execute(request);
        }
    }
}
