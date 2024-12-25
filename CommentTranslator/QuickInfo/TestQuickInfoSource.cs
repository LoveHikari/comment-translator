using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommentTranslator.Presentation;
using CommentTranslator.QuickInfo.comment;
using CommentTranslator.Util;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace CommentTranslator.QuickInfo
{
    internal class TestQuickInfoSource : IAsyncQuickInfoSource
    {
        private TestQuickInfoSourceProvider m_provider;
        private ITextBuffer m_subjectBuffer;
        private Dictionary<string, string> m_dictionary;
        private bool m_isDisposed;

        internal TestQuickInfoSource(TestQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            m_provider = provider;
            m_subjectBuffer = subjectBuffer;

            //these are the method names and their descriptions
            m_dictionary = new Dictionary<string, string>();
            m_dictionary.Add("add", "int add(int firstInt, int secondInt)\nAdds one integer to another.");
            m_dictionary.Add("subtract", "int subtract(int firstInt, int secondInt)\nSubtracts one integer from another.");
            m_dictionary.Add("multiply", "int multiply(int firstInt, int secondInt)\nMultiplies one integer by another.");
            m_dictionary.Add("divide", "int divide(int firstInt, int secondInt)\nDivides one integer by another.");

        }
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
        
        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            // Map the trigger point down to our buffer.
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(m_subjectBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue)
            {
                return null;
            }

            ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);
            var typeName = m_subjectBuffer.ContentType.TypeName;
            //look for occurrences of our QuickInfo words in the span
            // 使用 navigator.GetExtentOfWord 获取 XML、XAML 中的字符时会导致 VS 卡死
            //ITextStructureNavigator navigator = m_provider.NavigatorService.GetTextStructureNavigator(m_subjectBuffer);
            //TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);
            //string searchText = extent.Span.GetText();
            var snapshotPoint = subjectTriggerPoint.Value;
            var word = GetWord(snapshotPoint.Snapshot.GetText(), snapshotPoint.Position);

            var applicableToSpan = currentSnapshot.CreateTrackingSpan(querySpan, SpanTrackingMode.EdgeInclusive);
            var element = new ContainerElement(ContainerElementStyle.Stacked);

            if (CommentTranslatorPackage.Settings.AutoTranslateQuickInfo)
            {
                try
                {
                    var temp = await CommentTranslate.TryTranslateMethodInformationAsync(session, typeName);
                    if (temp != null && temp.Any())
                    {
                        var e = element.Elements.Append(new ClassifiedTextElement(temp));
                        element = new ContainerElement(ContainerElementStyle.Stacked, e);
                    }
                }
                catch (Exception ex)
                {
                    
                }
                
            }

            return new QuickInfoItem(applicableToSpan, element);
        }

        private string GetWord(string text, int position)
        {
            if (position < 0 || position >= text.Length)
            {
                return null;
            }

            // 获取单词边界
            int strat = position, end = position;
            while (strat >= 0)
            {
                if (char.IsWhiteSpace(text[strat]) || char.IsPunctuation(text[strat]) || char.IsSymbol(text[strat]))
                {
                    break;
                }
                strat--;
            }
            while (end < text.Length)
            {
                if (char.IsWhiteSpace(text[end]) || char.IsPunctuation(text[end]) || char.IsSymbol(text[end]))
                {
                    break;
                }
                end++;
            }

            if ((end > strat) == false)
            {
                return null;
            }

            // 返回单词
            return text.Substring(strat + 1, end - strat - 1);
        }

    }
}