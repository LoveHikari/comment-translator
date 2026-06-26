using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using CommentTranslator.QuickInfo.comment;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace CommentTranslator.QuickInfo
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name(CommentTranslate.ProviderName)]
    [Order(After = "Default Quick Info Presenter")]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    [ContentType("code")]
    [ContentType("projection")]
    internal class TestQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        [ImportMany(typeof(IAsyncQuickInfoSourceProvider))]
        internal IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderable>> PeerQuickInfoProviders { get; set; }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new TestQuickInfoSource(textBuffer, PeerQuickInfoProviders);
        }
    }
}
