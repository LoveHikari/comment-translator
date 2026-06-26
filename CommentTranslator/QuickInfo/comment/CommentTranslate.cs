using System;
using CommentTranslator.IntelliSense;
using CommentTranslator.QuickInfo;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Adornments;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace CommentTranslator.QuickInfo.comment
{
    internal class CommentTranslate
    {
        internal const string ProviderName = "ToolTip QuickInfo Source";

        /// <summary>
        /// 尝试翻译方法或变量的注释文本。
        /// </summary>
        public static async Task<IEnumerable<ClassifiedTextRun>> TryTranslateMethodInformationAsync(
            IAsyncQuickInfoSession session,
            string typeName,
            ITextBuffer subjectBuffer,
            IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderable>> otherProviders,
            CancellationToken cancellationToken = default)
        {
            string str = await TryGetMethodInformationAsync(
                    session,
                    typeName,
                    subjectBuffer,
                    otherProviders,
                    cancellationToken)
                .ConfigureAwait(false);

            return await TryTranslateExtractedTextAsync(str, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 翻译已提取的纯文本文档（供 sighelp-doc 缓冲区等路径复用）。
        /// </summary>
        public static async Task<IEnumerable<ClassifiedTextRun>> TryTranslateExtractedTextAsync(
            string str,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            IntelliSenseTranslationHelper.PretreatPlainText(ref str);
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            string translated = await IntelliSenseTranslationHelper.GetOrTranslateAsync(str, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(translated))
            {
                return IntelliSenseTranslationHelper.ComposeClassifiedRunsWithTranslation(translated);
            }

            return null;
        }

        /// <summary>
        /// 优先从已聚合的会话内容提取；若尚未就绪，则向其它 Quick Info 源查询文档文本。
        /// </summary>
        private static async Task<string> TryGetMethodInformationAsync(
            IAsyncQuickInfoSession session,
            string typeName,
            ITextBuffer subjectBuffer,
            IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderable>> otherProviders,
            CancellationToken cancellationToken)
        {
            if (string.Equals(subjectBuffer?.ContentType?.TypeName, "sighelp-doc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(subjectBuffer?.ContentType?.TypeName, "sighelp", StringComparison.OrdinalIgnoreCase))
            {
                string bufferText = QuickInfoDocumentationExtractor.TryExtractFromSubjectBuffer(subjectBuffer);
                if (!string.IsNullOrEmpty(bufferText))
                {
                    return bufferText;
                }
            }

            string text = QuickInfoDocumentationExtractor.TryExtract(session, typeName);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (otherProviders == null || subjectBuffer == null)
            {
                return null;
            }

            foreach (Lazy<IAsyncQuickInfoSourceProvider, IOrderable> lazy in OrderPeerProviders(otherProviders))
            {
                cancellationToken.ThrowIfCancellationRequested();

                IAsyncQuickInfoSource source = lazy.Value.TryCreateQuickInfoSource(subjectBuffer);
                if (source == null)
                {
                    continue;
                }

                using (source)
                {
                    QuickInfoItem item = await source.GetQuickInfoItemAsync(session, cancellationToken)
                        .ConfigureAwait(false);
                    text = QuickInfoDocumentationExtractor.TryExtractFromQuickInfoItem(item, typeName);
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderable>> OrderPeerProviders(
            IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderable>> providers)
        {
            return providers
                .Where(p => p.Metadata?.Name != ProviderName
                    && p.Metadata?.Name != "Default Quick Info Presenter")
                .OrderBy(p => GetProviderPriority(p.Metadata?.Name));
        }

        /// <summary>
        /// IOrderable 仅含 Name/Before/After，无 Order；按名称启发式优先语言服务 Provider。
        /// </summary>
        private static int GetProviderPriority(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 100;
            }

            if (name.IndexOf("Roslyn", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Semantic", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("CSharp", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0;
            }

            if (string.Equals(name, "default", System.StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return 50;
        }
    }
}
