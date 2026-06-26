using System;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace CommentTranslator.IntelliSense
{
    /// <summary>
    /// 判断补全项 <see cref="CompletionItem.DisplayText"/> 是否适合翻译，并在安全条件下返回译文。
    /// 不修改 InsertText、FilterText、SortText，避免破坏补全匹配与提交行为。
    /// </summary>
    internal static class CompletionDisplayTextHelper
    {
        /// <summary>
        /// 每轮补全会话内最多对多少项尝试行内 DisplayText 翻译（含阻塞 API 调用）。
        /// </summary>
        public const int MaxDisplayTranslationsPerSession = 20;

        public static bool IsDisplayTranslationEnabled()
        {
            return CommentTranslatorPackage.Settings != null
                && CommentTranslatorPackage.Settings.AutoTranslateCompletionDisplay
                && CommentTranslatorPackage.TranslateClient != null;
        }

        public static bool ShouldTranslateDisplayText(CompletionItem item)
        {
            if (!IsDisplayTranslationEnabled() || item == null)
            {
                return false;
            }

            string display = item.DisplayText;
            string insert = item.InsertText ?? display;
            if (string.IsNullOrWhiteSpace(display) || string.Equals(display, insert, StringComparison.Ordinal))
            {
                return false;
            }

            return LooksLikeNaturalLanguage(display);
        }

        /// <summary>
        /// 含空格或非标标识符字符的文本视为描述性展示文本；纯符号名（如 WriteLine）不翻译。
        /// </summary>
        internal static bool LooksLikeNaturalLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();
            if (text.IndexOf(' ') >= 0 || text.IndexOf('\t') >= 0)
            {
                return true;
            }

            return !IsSimpleIdentifier(text);
        }

        private static bool IsSimpleIdentifier(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
                {
                    return false;
                }
            }

            return text.Length > 0;
        }

        public static string TryGetTranslatedDisplayText(string displayText)
        {
            if (string.IsNullOrWhiteSpace(displayText))
            {
                return null;
            }

            string plain = displayText;
            IntelliSenseTranslationHelper.PretreatPlainText(ref plain);
            if (string.IsNullOrEmpty(plain))
            {
                return null;
            }

            return IntelliSenseTranslationHelper.GetOrTranslateBlocking(plain);
        }
    }
}
