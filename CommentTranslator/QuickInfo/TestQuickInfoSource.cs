using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommentTranslator.QuickInfo.comment;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

namespace CommentTranslator.QuickInfo
{
    internal class TestQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer m_subjectBuffer;
        private readonly IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderable>> m_peerProviders;
        private bool m_isDisposed;

        internal TestQuickInfoSource(
            ITextBuffer subjectBuffer,
            IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderable>> peerProviders)
        {
            m_subjectBuffer = subjectBuffer;
            m_peerProviders = peerProviders ?? Enumerable.Empty<Lazy<IAsyncQuickInfoSourceProvider, IOrderable>>();
        }

        public void Dispose()
        {
            if (!m_isDisposed)
            {
                m_isDisposed = true;
            }
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(m_subjectBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue)
            {
                return null;
            }

            ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);
            var typeName = m_subjectBuffer.ContentType.TypeName;
            var applicableToSpan = currentSnapshot.CreateTrackingSpan(querySpan, SpanTrackingMode.EdgeInclusive);

            if (ShouldTranslate(m_subjectBuffer))
            {
                if (QuickInfoDocumentationExtractor.SessionAlreadyHasTranslation(session))
                {
                    return null;
                }

                if (!QuickInfoSessionTranslationGuard.TryClaimSourceBuffer(session, m_subjectBuffer))
                {
                    return null;
                }

                try
                {
                    var temp = await CommentTranslate.TryTranslateMethodInformationAsync(
                        session,
                        typeName,
                        m_subjectBuffer,
                        m_peerProviders,
                        cancellationToken);
                    if (temp != null && temp.Any()
                        && QuickInfoSessionTranslationGuard.TryCompleteContribution(session, m_subjectBuffer))
                    {
                        var element = new ContainerElement(
                            ContainerElementStyle.Stacked,
                            new ClassifiedTextElement(temp));
                        return new QuickInfoItem(applicableToSpan, element);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // 翻译失败时不影响原有 Quick Info 展示。
                }
            }

            return null;
        }

        /// <summary>
        /// 由 <see cref="Settings.AutoTranslateQuickInfo"/> 控制（签名帮助文档由 adornment 层单独处理）。
        /// </summary>
        private static bool ShouldTranslate(ITextBuffer subjectBuffer)
        {
            var settings = CommentTranslatorPackage.Settings;
            if (settings == null || CommentTranslatorPackage.TranslateClient == null)
            {
                return false;
            }

            return settings.AutoTranslateQuickInfo
                && QuickInfoSessionTranslationGuard.GetBufferPriority(subjectBuffer.ContentType?.TypeName) < 10;
        }
    }
}