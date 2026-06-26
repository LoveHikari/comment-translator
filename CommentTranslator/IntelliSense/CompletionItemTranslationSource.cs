using System;
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;

namespace CommentTranslator.IntelliSense
{
    /// <summary>
    /// 伪造的 <see cref="IAsyncCompletionSource"/>：仅用于作为包装项的 <see cref="CompletionItem.Source"/>，
    /// 以便在 <see cref="IAsyncCompletionSource.GetDescriptionAsync"/> 中拦截并翻译原有说明。
    /// 不参与 InitializeCompletion / GetCompletionContextAsync，避免干扰语言服务自身补全逻辑。
    /// </summary>
    internal sealed class CompletionItemTranslationSource : IAsyncCompletionSource
    {
        /// <summary>
        /// 在包装项的属性袋中保存原始 <see cref="CompletionItem"/> 的键（使用专用类型避免与其它属性冲突）。
        /// </summary>
        internal static readonly object OriginalCompletionItemKey = typeof(CompletionItemTranslationSource);

        internal static readonly CompletionItemTranslationSource Instance = new CompletionItemTranslationSource();

        private CompletionItemTranslationSource()
        {
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            // 本类型不会通过 MEF 注册为补全发起方；若被调用则明确声明不参与。
            return CompletionStartData.DoesNotParticipateInCompletion;
        }

        public Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            return Task.FromResult(new CompletionContext(ImmutableArray<CompletionItem>.Empty));
        }

        // 说明：VS 17.12+ 的 IAsyncCompletionSource 仅保留带 IAsyncCompletionSession 的 GetCompletionContextAsync；
        // 早期 SDK 中的 InitialTrigger 重载与 InitialTrigger 类型已移除，勿再实现以免 CS0246。

        public bool TryGetApplicableToSpan(char typeChar, SnapshotPoint location, out SnapshotSpan applicableToSpan, CancellationToken token)
        {
            applicableToSpan = default;
            return false;
        }

        public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken cancellationToken)
        {
            if (!TryGetOriginalItem(item, out CompletionItem original))
            {
                return null;
            }

            object innerDescription = await InvokeInnerGetDescriptionAsync(session, item, original, cancellationToken)
                .ConfigureAwait(false);

            if (!ShouldTranslate() || innerDescription == null)
            {
                return innerDescription;
            }

            string plain = AdornmentPlainTextExtractor.TryExtractPlainText(innerDescription);
            if (string.IsNullOrEmpty(plain))
            {
                return innerDescription;
            }

            IntelliSenseTranslationHelper.PretreatPlainText(ref plain);
            if (string.IsNullOrEmpty(plain))
            {
                return innerDescription;
            }

            string translated = await IntelliSenseTranslationHelper.GetOrTranslateAsync(plain, cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrEmpty(translated))
            {
                return innerDescription;
            }

            return IntelliSenseTranslationHelper.ComposeStackedWithTranslation(innerDescription, translated);
        }

        public Task<object> GetDescriptionAsync(CompletionItem item, CancellationToken cancellationToken)
        {
            return GetDescriptionAsync(null, item, cancellationToken);
        }

        /// <summary>
        /// 克隆 <see cref="CompletionItem"/> 并将 <see cref="OriginalCompletionItemKey"/> 附着到属性袋。
        /// </summary>
        internal static CompletionItem WrapItem(CompletionItem original, ref int displayTranslateBudget)
        {
            if (original == null || ReferenceEquals(original.Source, Instance))
            {
                return original;
            }

            string displayText = original.DisplayText;
            if (CompletionDisplayTextHelper.ShouldTranslateDisplayText(original) && displayTranslateBudget > 0)
            {
                string translatedDisplay = CompletionDisplayTextHelper.TryGetTranslatedDisplayText(original.DisplayText);
                if (!string.IsNullOrEmpty(translatedDisplay))
                {
                    displayText = translatedDisplay;
                    displayTranslateBudget--;
                }
            }

            var wrapped = CloneCompletionItem(original, displayText);
            CopyPropertyBag(original, wrapped);
            wrapped.Properties.AddProperty(OriginalCompletionItemKey, original);
            return wrapped;
        }

        internal static CompletionItem WrapItem(CompletionItem original)
        {
            int budget = 0;
            return WrapItem(original, ref budget);
        }

        private static bool TryGetOriginalItem(CompletionItem item, out CompletionItem original)
        {
            return item.Properties.TryGetProperty(OriginalCompletionItemKey, out original) && original != null;
        }

        private static bool ShouldTranslate()
        {
            return CommentTranslatorPackage.Settings != null
                && CommentTranslatorPackage.Settings.AutoTranslateCompletion
                && CommentTranslatorPackage.TranslateClient != null;
        }

        private static async Task<object> InvokeInnerGetDescriptionAsync(
            IAsyncCompletionSession session,
            CompletionItem wrappedItem,
            CompletionItem original,
            CancellationToken cancellationToken)
        {
            if (original.Source == null)
            {
                return null;
            }

            // 优先使用列表中的包装项（属性袋已复制）；部分语言服务按 item 实例查找符号。
            object description = await original.Source
                .GetDescriptionAsync(session, wrappedItem, cancellationToken)
                .ConfigureAwait(false);
            if (description != null)
            {
                return description;
            }

            return await original.Source
                .GetDescriptionAsync(session, original, cancellationToken)
                .ConfigureAwait(false);
        }

        private static CompletionItem CloneCompletionItem(CompletionItem item, string displayText = null)
        {
            return new CompletionItem(
                displayText ?? item.DisplayText,
                Instance,
                item.Icon,
                item.Filters,
                item.InsertText,
                item.SortText,
                item.FilterText,
                item.Suffix ?? string.Empty,
                item.AutomationText ?? string.Empty,
                item.AttributeIcons,
                item.CommitCharacters,
                item.ApplicableToSpan,
                item.IsPreselected,
                item.IsCommittedAsSnippet);
        }

        /// <summary>
        /// 将语言服务在原始项上附加的属性复制到包装项，避免提交或内部逻辑丢失符号等信息。
        /// 使用反射访问 PropertyList，以兼容不同 SDK 版本的可见性差异。
        /// </summary>
        private static void CopyPropertyBag(CompletionItem from, CompletionItem to)
        {
            object properties = from.Properties;
            if (properties == null)
            {
                return;
            }

            try
            {
                PropertyInfo listProperty = properties.GetType().GetProperty(
                    "PropertyList",
                    BindingFlags.Public | BindingFlags.Instance);
                object listObj = listProperty?.GetValue(properties);
                if (!(listObj is IEnumerable listEnumerable))
                {
                    return;
                }

                foreach (var entry in listEnumerable)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    Type entryType = entry.GetType();
                    object key = entryType.GetProperty("Key")?.GetValue(entry);
                    object value = entryType.GetProperty("Value")?.GetValue(entry);
                    if (key == null)
                    {
                        continue;
                    }

                    to.Properties.AddProperty(key, value);
                }
            }
            catch (ArgumentException)
            {
                // AddProperty 对重复键会抛异常；此时保留已复制部分即可。
            }
        }
    }
}
