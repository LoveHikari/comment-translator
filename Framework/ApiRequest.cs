namespace Framework
{
    public class ApiRequest
    {
        /// <summary>
        /// 翻译服务器类型
        /// </summary>
        public TranslateServerEnum TranslateServer { get; set; }
        /// <summary>
        /// 请求语言
        /// </summary>
        public LanguageEnum FromLanguage { get; set; }
        /// <summary>
        /// 目标语言
        /// </summary>
        public LanguageEnum ToLanguage { get; set; }
        /// <summary>
        /// 翻译内容
        /// </summary>
        public string Body { get; set; }
    }
}
