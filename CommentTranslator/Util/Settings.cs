using CommentTranslator.Presentation;
using Framework;

namespace CommentTranslator.Util
{
    /// <summary>
    /// 获取工具菜单设置的值
    /// </summary>
    public class Settings
    {
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
        }
    }
}
