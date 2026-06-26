using System;
using System.Collections.Generic;
using System.Linq;
using CommentTranslator.IntelliSense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace CommentTranslator.QuickInfo
{
    /// <summary>
    /// 从 Quick Info 会话（旧式 / 新式 API）中提取可翻译的 XML 文档说明文本。
    /// </summary>
    internal static class QuickInfoDocumentationExtractor
    {
        public static bool SessionAlreadyHasTranslation(IAsyncQuickInfoSession session)
        {
            if (session?.Content == null)
            {
                return false;
            }

            foreach (object item in session.Content)
            {
                if (ContainsTranslationMarker(item))
                {
                    return true;
                }
            }

            return false;
        }

        public static string TryExtract(IAsyncQuickInfoSession session, string typeName)
        {
            if (session == null)
            {
                return null;
            }

            string text = TryExtractFromLegacyProperties(session, typeName);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            text = TryExtractFromSessionContent(session);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            return null;
        }

        /// <summary>
        /// 从 <c>sighelp-doc</c> / <c>sighelp</c> 缓冲区全文提取可翻译文档（跳过签名行）。
        /// </summary>
        public static string TryExtractFromSubjectBuffer(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            return TryExtractFromSignatureHelpText(buffer.CurrentSnapshot.GetText());
        }

        /// <summary>
        /// 签名帮助视图常把签名行与 XML 文档放在同一缓冲区；按行过滤签名行后拼接文档。
        /// </summary>
        public static string TryExtractFromSignatureHelpText(string fullText)
        {
            if (string.IsNullOrWhiteSpace(fullText))
            {
                return null;
            }

            var parts = new System.Text.StringBuilder();
            string[] lines = fullText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || IsLikelySignatureLine(trimmed))
                {
                    continue;
                }

                if (parts.Length > 0)
                {
                    parts.AppendLine();
                }

                parts.Append(trimmed);
            }

            string result = parts.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public static string TryExtractFromQuickInfoItem(object item, string typeName)
        {
            if (item == null)
            {
                return null;
            }

            if (item is QuickInfoItem quickInfoItem)
            {
                return TryExtractFromContentObject(quickInfoItem.Item, typeName);
            }

            return TryExtractFromContentObject(item, typeName);
        }

        private static string TryExtractFromLegacyProperties(IAsyncQuickInfoSession session, string typeName)
        {
            if (session.Properties?.PropertyList == null || session.Properties.PropertyList.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < session.Properties.PropertyList.Count; i++)
            {
                if (session.Properties.PropertyList[i].Value is IQuickInfoSession legacy)
                {
                    string text = TryExtractFromLegacySession(legacy, typeName);
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
            }

            return null;
        }

        private static string TryExtractFromSessionContent(IAsyncQuickInfoSession session)
        {
            if (session.Content == null)
            {
                return null;
            }

            string best = null;
            foreach (object item in session.Content)
            {
                string plain = TryExtractFromContentObject(item, null);
                if (!string.IsNullOrEmpty(plain) && (best == null || plain.Length > best.Length))
                {
                    best = plain;
                }
            }

            return best;
        }

        private static string TryExtractFromLegacySession(IQuickInfoSession quick, string typeName)
        {
            if (quick?.QuickInfoContent == null)
            {
                return null;
            }

            if (typeName == "C/C++")
            {
                foreach (object top in quick.QuickInfoContent)
                {
                    if (top is ContainerElement container)
                    {
                        string cpp = TryGetCppMethodInformation(container.Elements);
                        if (!string.IsNullOrEmpty(cpp))
                        {
                            return cpp;
                        }
                    }
                }
            }

            string best = null;
            foreach (object top in quick.QuickInfoContent)
            {
                string plain = TryExtractFromContentObject(top, typeName);
                if (!string.IsNullOrEmpty(plain) && (best == null || plain.Length > best.Length))
                {
                    best = plain;
                }
            }

            return best;
        }

        private static string TryExtractFromContentObject(object node, string typeName)
        {
            if (node == null)
            {
                return null;
            }

            if (node is ClassifiedTextElement classified)
            {
                return ExtractIfDocumentation(classified);
            }

            if (node is ContainerElement container)
            {
                return TryExtractFromContainer(container);
            }

            return ExtractIfDocumentation(node);
        }

        private static string TryExtractFromContainer(ContainerElement container)
        {
            string best = null;
            foreach (object child in container.Elements)
            {
                string plain;
                if (child is ClassifiedTextElement classified)
                {
                    plain = ExtractIfDocumentation(classified);
                }
                else if (child is ContainerElement nested)
                {
                    plain = TryExtractFromContainer(nested);
                }
                else
                {
                    plain = ExtractIfDocumentation(child);
                }

                if (!string.IsNullOrEmpty(plain) && (best == null || plain.Length > best.Length))
                {
                    best = plain;
                }
            }

            return best;
        }

        private static string ExtractIfDocumentation(object node)
        {
            string plain = AdornmentPlainTextExtractor.TryExtractPlainText(node);
            if (string.IsNullOrEmpty(plain) || IsLikelySignatureLine(plain) || ContainsTranslationMarker(plain))
            {
                return null;
            }

            return plain;
        }

        private static bool ContainsTranslationMarker(object node)
        {
            string plain = AdornmentPlainTextExtractor.TryExtractPlainText(node);
            return ContainsTranslationMarker(plain);
        }

        private static bool ContainsTranslationMarker(string plain)
        {
            return !string.IsNullOrEmpty(plain)
                && plain.IndexOf("[译]", StringComparison.Ordinal) >= 0;
        }

        private static string ExtractIfDocumentation(ClassifiedTextElement classified)
        {
            return ExtractIfDocumentation((object)classified);
        }

        internal static bool IsLikelySignatureLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            string trimmed = text.Trim();
            if (trimmed.Length >= 160)
            {
                return false;
            }

            if (trimmed.Contains("(") && trimmed.Contains(")"))
            {
                return true;
            }

            if (trimmed.StartsWith("public ") ||
                trimmed.StartsWith("private ") ||
                trimmed.StartsWith("protected ") ||
                trimmed.StartsWith("internal ") ||
                trimmed.StartsWith("static ") ||
                trimmed.StartsWith("void ") ||
                trimmed.StartsWith("class ") ||
                trimmed.StartsWith("struct ") ||
                trimmed.StartsWith("enum ") ||
                trimmed.StartsWith("readonly ") ||
                trimmed.StartsWith("virtual ") ||
                trimmed.StartsWith("override "))
            {
                return true;
            }

            return false;
        }

        private static string TryGetCppMethodInformation(IEnumerable<object> elements)
        {
            var list = new List<object>();
            foreach (object item in elements)
            {
                list.Add(item);
            }

            if (list.Count <= 3 || !(list[2] is ContainerElement docContainer))
            {
                return null;
            }

            if (docContainer.Elements.Count() != 1 || !(docContainer.Elements.ElementAt(0) is ClassifiedTextElement textElement))
            {
                return null;
            }

            if (textElement.Runs.Count() == 1)
            {
                return textElement.Runs.ElementAt(0).Text;
            }

            if (textElement.Runs.Count() > 1)
            {
                if (textElement.Runs.ElementAt(0).Text.IndexOf("扩展到:") != -1 ||
                    textElement.Runs.ElementAt(0).Text.IndexOf("大小:") != -1)
                {
                    return null;
                }

                var sb = new System.Text.StringBuilder();
                foreach (var run in textElement.Runs)
                {
                    var temp = run.Text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", string.Empty);
                    IntelliSenseTranslationHelper.PretreatPlainText(ref temp);
                    sb.Append(temp);
                }

                return sb.ToString();
            }

            return null;
        }
    }
}
