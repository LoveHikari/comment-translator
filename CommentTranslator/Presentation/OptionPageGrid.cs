using System.ComponentModel;
using CommentTranslator.Resources.lang;
using CommentTranslator.Util;
using Framework;
using Microsoft.VisualStudio.Shell;

namespace CommentTranslator.Presentation
{
    /// <summary>
    /// 工具选项窗体
    /// </summary>
    public class OptionPageGrid : DialogPage
    {
        [Category("Server")]
        [LocalizedDisplayName("TranslateServer_DisplayName", typeof(Resource))]
        [LocalizedDescription("TranslateServer_Description", typeof(Resource))]
        public TranslateServerEnum TranslateServer { get; set; } = TranslateServerEnum.Google;

        /// <summary>
        /// Gets or sets a value indicating whether 待翻译语言
        /// </summary>
        [Category("Translate")]
        [LocalizedDisplayName("TranslateFrom_DisplayName", typeof(Resource))]
        [LocalizedDescription("TranslateFrom_Description", typeof(Resource))]
        public LanguageEnum TranslateFrom { get; set; } = LanguageEnum.Auto;

        /// <summary>
        /// Gets or sets a value indicating whether 目标语言
        /// </summary>
        [Category("Translate")]
        [LocalizedDisplayName("TranslatetTo_DisplayName", typeof(Resource))]
        [LocalizedDescription("TranslatetTo_Description", typeof(Resource))]
        public LanguageEnum TranslatetTo { get; set; } = GetCurrentCulture();

        ///// <summary>
        ///// Gets or sets a value indicating whether 自动检测语言
        ///// </summary>
        //[Category("Translate")]
        //[DisplayName("自动检测类型")]
        //[Description("自动检测待翻译语言类型")]
        //public bool AutoDetect { get; set; } = false;

        [Category("Translate")]
        [LocalizedDisplayName("AutoTranslateComment_DisplayName", typeof(Resource))]
        [LocalizedDescription("AutoTranslateComment_Description", typeof(Resource))]
        public bool AutoTranslateComment { get; set; } = false;

        [Category("Translate")]
        [LocalizedDisplayName("AutoTextCopy_DisplayName", typeof(Resource))]
        [LocalizedDescription("AutoTextCopy_Description", typeof(Resource))]
        public bool AutoTextCopy { get; set; } = false;

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                this.SaveToSetting();
            }
        }


        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveToSetting()
        {
            // C#中MessageBox用法大全（附效果图）
            // https://www.cnblogs.com/rooly/articles/1910063.html
            //if (string.IsNullOrWhiteSpace(TKK))
            //{
            //    MessageBox.Show("请先设置TKK值！", "系统提示");

            //    // return;
            //}

            // 刷新值
            CommentTranslatorPackage.Settings.ReloadSetting(this);
        }

        // 【小试插件开发】给Visual Studio装上自己定制的功能来提高代码调试效率
        // https://www.cnblogs.com/hohoa/p/6617619.html?utm_source=gold_browser_extension

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