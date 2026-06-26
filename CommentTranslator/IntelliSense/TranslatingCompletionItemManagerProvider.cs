using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CommentTranslator.IntelliSense
{
    /// <summary>
    /// 在默认补全列表管理器之外包装一层，仅用于注入说明翻译，不替换 VS 内置过滤/匹配逻辑。
    /// </summary>
    [Export(typeof(IAsyncCompletionItemManagerProvider))]
    [Name("Comment Translator completion item manager")]
    [ContentType("text")]
    [ContentType("code")]
    [ContentType("projection")]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    [Order(Before = PredefinedCompletionNames.DefaultCompletionItemManager)]
    internal sealed class TranslatingCompletionItemManagerProvider : IAsyncCompletionItemManagerProvider
    {
        [ImportMany(typeof(IAsyncCompletionItemManagerProvider))]
        private IEnumerable<Lazy<IAsyncCompletionItemManagerProvider, IOrderable>> ItemManagerProviders { get; set; }

        public IAsyncCompletionItemManager GetOrCreate(ITextView textView)
        {
            IAsyncCompletionItemManager defaultManager = ResolveDefaultManager(textView);
            return new TranslatingCompletionItemManager(defaultManager);
        }

        private IAsyncCompletionItemManager ResolveDefaultManager(ITextView textView)
        {
            Lazy<IAsyncCompletionItemManagerProvider, IOrderable> defaultProvider = ItemManagerProviders
                ?.FirstOrDefault(p => string.Equals(
                    p.Metadata?.Name,
                    PredefinedCompletionNames.DefaultCompletionItemManager,
                    StringComparison.Ordinal));

            if (defaultProvider == null)
            {
                throw new InvalidOperationException(
                    "Default IAsyncCompletionItemManagerProvider was not found in MEF composition.");
            }

            return defaultProvider.Value.GetOrCreate(textView);
        }
    }
}
