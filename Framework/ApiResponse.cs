using System.Collections.Generic;

namespace Framework
{
    public class ApiResponse
    {
        /// <summary>
        /// 待翻译文件
        /// </summary>
        public string SourceText { get; set; }
        /// <summary>
        /// 翻译后的文本
        /// </summary>
        public string TargetText { get; set; }
        /// <summary>
        /// 请求语言
        /// </summary>
        public string FromLanguage { get; set; }
        /// <summary>
        /// 目标语言
        /// </summary>
        public string ToLanguage { get; set; }
        /// <summary>
        /// 翻译是否成功
        /// </summary>
        public bool Success { get; set; }

        public int Code { get; set; }
        public string Message { get; set; }
    }
}
