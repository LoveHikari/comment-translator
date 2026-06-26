using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace CommentTranslator.IntelliSense
{
    /// <summary>
    /// 包装默认补全列表管理器：过滤/排序完全委托给 VS 内置实现，
    /// 仅在 <see cref="UpdateCompletionListAsync"/> 返回前包装可见项以注入说明翻译。
    /// 实现 <see cref="IAsyncCompletionItemManager2"/> 以兼容 VS 2022+ / VS 2026 的匹配逻辑。
    /// </summary>
    internal sealed class TranslatingCompletionItemManager : IAsyncCompletionItemManager2
    {
        private readonly IAsyncCompletionItemManager _inner;

        public TranslatingCompletionItemManager(IAsyncCompletionItemManager inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<FilteredCompletionModel> UpdateCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionDataSnapshot data,
            CancellationToken token)
        {
            FilteredCompletionModel model = _inner is IAsyncCompletionItemManager2 inner2
                ? await inner2.UpdateCompletionListAsync(session, data, token).ConfigureAwait(false)
                : await _inner.UpdateCompletionListAsync(session, data, token).ConfigureAwait(false);

            return WrapFilteredModel(model);
        }

        public Task<ImmutableArray<CompletionItem>> SortCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken token)
        {
            return _inner.SortCompletionListAsync(session, data, token);
        }

        public async Task<CompletionList<CompletionItem>> SortCompletionItemListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken token)
        {
            if (_inner is IAsyncCompletionItemManager2 inner2)
            {
                return await inner2.SortCompletionItemListAsync(session, data, token).ConfigureAwait(false);
            }

            ImmutableArray<CompletionItem> array = await _inner
                .SortCompletionListAsync(session, data, token)
                .ConfigureAwait(false);
            return session.CreateCompletionList(array);
        }

        private static FilteredCompletionModel WrapFilteredModel(FilteredCompletionModel model)
        {
            var settings = CommentTranslatorPackage.Settings;
            if (settings == null
                || (!settings.AutoTranslateCompletion && !settings.AutoTranslateCompletionDisplay))
            {
                return model;
            }

            ImmutableArray<CompletionItemWithHighlight> items = model.Items;
            if (items.IsDefaultOrEmpty)
            {
                return model;
            }

            int displayTranslateBudget = CompletionDisplayTextHelper.MaxDisplayTranslationsPerSession;
            ImmutableArray<CompletionItemWithHighlight>.Builder builder =
                ImmutableArray.CreateBuilder<CompletionItemWithHighlight>(items.Length);

            foreach (CompletionItemWithHighlight itemWithHighlight in items)
            {
                CompletionItem wrapped = CompletionItemTranslationSource.WrapItem(
                    itemWithHighlight.CompletionItem,
                    ref displayTranslateBudget);
                builder.Add(new CompletionItemWithHighlight(wrapped, itemWithHighlight.HighlightedSpans));
            }

            CompletionItem uniqueItem = model.UniqueItem != null
                ? CompletionItemTranslationSource.WrapItem(model.UniqueItem, ref displayTranslateBudget)
                : null;

            return new FilteredCompletionModel(
                builder.ToImmutable(),
                model.SelectedItemIndex,
                model.Filters,
                model.SelectionHint,
                model.CenterSelection,
                uniqueItem);
        }
    }
}
