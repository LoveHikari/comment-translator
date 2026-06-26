using CommentTranslator.Presentation;
using Framework;

namespace CommentTranslator.Util
{
    /// <summary>
    /// 获取工具菜单设置的值
    /// </summary>
    public class Settings
    {
        public Settings()
        {
            this.TranslateServer = TranslateServerEnum.Bing;
            this.TranslateFrom = LanguageEnum.Auto;
            this.TranslatetTo = GetCurrentCulture();
            this.AutoTranslateComment = false;
            this.AutoTextCopy = false;
            this.AutoTranslateQuickInfo = true;
            this.AutoTranslateCompletion = true;
            this.AutoTranslateSignatureHelp = true;
            this.AutoTranslateCompletionDisplay = false;
        }
        /// <summary>
        /// 翻译服务器
        /// </summary>
        public TranslateServerEnum TranslateServer { get; set; }
        /// <summary>
        /// 待翻译语言
        /// </summary>
        public LanguageEnum TranslateFrom { get; set; }

        /// <summary>
        /// 目标语言
        /// </summary>
        public LanguageEnum TranslatetTo { get; set; }
        /// <summary>
        /// 打开文件自动翻译
        /// </summary>
        public bool AutoTranslateComment { get; set; }
        /// <summary>
        /// 手动翻译自动复制
        /// </summary>
        public bool AutoTextCopy { get; set; }
        /// <summary>
        /// 翻译快速信息文本
        /// </summary>
        public bool AutoTranslateQuickInfo { get; set; }
        /// <summary>
        /// 翻译 IntelliSense 补全列表中条目的说明（XML 文档/摘要等）
        /// </summary>
        public bool AutoTranslateCompletion { get; set; }
        /// <summary>
        /// 翻译签名帮助中的方法文档与参数说明（不改动签名行文本，以免破坏参数高亮）
        /// </summary>
        public bool AutoTranslateSignatureHelp { get; set; }
        /// <summary>
        /// 翻译补全列表中含描述性内容的 DisplayText（不改动 InsertText/FilterText/SortText；默认关闭）
        /// </summary>
        public bool AutoTranslateCompletionDisplay { get; set; }
        /// <summary>
        /// 刷新设置的值
        /// </summary>
        /// <param name="page"></param>
        public void ReloadSetting(OptionPageGrid page)
        {
            this.TranslateServer = page.TranslateServer;
            this.TranslateFrom = page.TranslateFrom;
            this.TranslatetTo = page.TranslatetTo;
            this.AutoTranslateComment = page.AutoTranslateComment;
            this.AutoTextCopy = page.AutoTextCopy;
            this.AutoTranslateQuickInfo = page.AutoTranslateQuickInfo;
            this.AutoTranslateCompletion = page.AutoTranslateCompletion;
            this.AutoTranslateSignatureHelp = page.AutoTranslateSignatureHelp;
            this.AutoTranslateCompletionDisplay = page.AutoTranslateCompletionDisplay;
        }
        /// <summary>
        /// 获得当前语言
        /// </summary>
        /// <returns></returns>
        private static LanguageEnum GetCurrentCulture()
        {
            string currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            switch (currentCulture)
            {
                case "ja-JP":
                    return LanguageEnum.日本語;
                case "zh-CN":
                    return LanguageEnum.简体中文;
                case "zh-TW":
                    return LanguageEnum.繁體中文;
                case "en-US":
                    return LanguageEnum.English;
                default:
                    return LanguageEnum.简体中文;
            }
        }
    }
}
