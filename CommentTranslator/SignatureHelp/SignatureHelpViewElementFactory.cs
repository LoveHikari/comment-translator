using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommentTranslator.IntelliSense;
using CommentTranslator.QuickInfo;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CommentTranslator.SignatureHelp
{
    /// <summary>
    /// VS 2022+ 签名帮助文档常通过 <see cref="IViewElementFactory"/> 渲染 WPF，
    /// 而非 <c>sighelp-doc</c> 文本缓冲区；在此路径下追加 [译] 块。
    /// </summary>
    [Export(typeof(IViewElementFactory))]
    [Name("Comment Translator Signature Help View Element Factory")]
    [TypeConversion(from: typeof(object), to: typeof(UIElement))]
    [Order(Before = "Default object converter")]
    [Order(Before = "Default Container Element View Factory")]
    [Order(Before = "Default ClassifiedText Element View Factory")]
    [ContentType("sighelp-doc")]
    [ContentType("sighelp")]
    [ContentType("sighelp-documentation")]
    internal sealed class SignatureHelpViewElementFactory : IViewElementFactory
    {
        static SignatureHelpViewElementFactory()
        {
            SignatureHelpDiagnostics.Log("SignatureHelpViewElementFactory MEF part loaded");
        }

        [ImportMany(typeof(IViewElementFactory))]
        internal IEnumerable<Lazy<IViewElementFactory, IOrderable>> ViewElementFactories { get; set; }

        public TView CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            string contentType = textView?.TextBuffer?.ContentType?.TypeName ?? "(null)";
            string modelType = model?.GetType().Name ?? "(null)";

            if (!IsUiElementView<TView>())
            {
                SignatureHelpDiagnostics.LogFormat(
                    "Factory: delegate (TView={0}, contentType={1}, model={2})",
                    typeof(TView).Name,
                    contentType,
                    modelType);
                return CreateInnerElement<TView>(textView, model);
            }

            string wrapReason;
            if (!ShouldWrapModel(model, out wrapReason))
            {
                SignatureHelpDiagnostics.LogFormat(
                    "Factory: skip wrap — {0} (contentType={1}, model={2})",
                    wrapReason,
                    contentType,
                    modelType);
                return CreateInnerElement<TView>(textView, model);
            }

            SignatureHelpDiagnostics.LogFormat(
                "Factory: wrapping (contentType={0}, model={1}, text={2})",
                contentType,
                modelType,
                SignatureHelpDiagnostics.Truncate(AdornmentPlainTextExtractor.TryExtractPlainText(model)));

            UIElement inner = CreateInnerElement<UIElement>(textView, model);
            if (inner == null)
            {
                SignatureHelpDiagnostics.Log("Factory: inner element is null");
                return null;
            }

            string plain = AdornmentPlainTextExtractor.TryExtractPlainText(model);
            if (string.IsNullOrEmpty(plain))
            {
                return inner as TView;
            }

            string source = plain;
            IntelliSenseTranslationHelper.PretreatPlainText(ref source);
            if (string.IsNullOrEmpty(source))
            {
                return inner as TView;
            }

            var panel = new StackPanel();
            panel.Children.Add(inner);

            var translationBlock = new SignatureHelpDocTranslationAdornment(null);
            panel.Children.Add(translationBlock);

            string cached = IntelliSenseTranslationHelper.TryGetCachedTranslation(source);
            if (!string.IsNullOrEmpty(cached))
            {
                SignatureHelpDiagnostics.LogFormat("Factory: cache hit, len={0}", cached.Length);
                translationBlock.Update(cached);
                return panel as TView;
            }

            SignatureHelpDiagnostics.Log("Factory: requesting translation async");
            int generation = Interlocked.Increment(ref translationBlock.TranslateGeneration);
            string capture = source;
            _ = Task.Run(async () =>
            {
                try
                {
                    string translated = await IntelliSenseTranslationHelper
                        .GetOrTranslateAsync(capture, CancellationToken.None)
                        .ConfigureAwait(false);
                    if (string.IsNullOrEmpty(translated))
                    {
                        SignatureHelpDiagnostics.Log("Factory: async returned empty");
                        return;
                    }

                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        if (generation == translationBlock.TranslateGeneration)
                        {
                            SignatureHelpDiagnostics.LogFormat(
                                "Factory: async ok, len={0}",
                                translated.Length);
                            translationBlock.Update(translated);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    SignatureHelpDiagnostics.Log("Factory: async error — " + ex.Message);
                }
            });

            return panel as TView;
        }

        private static bool IsUiElementView<TView>() where TView : class
        {
            return typeof(TView) == typeof(UIElement)
                || typeof(FrameworkElement).IsAssignableFrom(typeof(TView));
        }

        private static bool ShouldWrapModel(object model, out string skipReason)
        {
            skipReason = null;
            if (!SignatureHelpDocTranslationHelper.IsEnabled())
            {
                skipReason = "disabled";
                return false;
            }

            if (model == null)
            {
                skipReason = "model is null";
                return false;
            }

            // 仅包装叶子 ClassifiedTextElement，避免父 Container 与子节点重复追加 [译]。
            if (model is ContainerElement)
            {
                skipReason = "container element";
                return false;
            }

            if (!(model is ClassifiedTextElement || model is string))
            {
                skipReason = "unsupported model type: " + model.GetType().FullName;
                return false;
            }

            string plain = AdornmentPlainTextExtractor.TryExtractPlainText(model);
            if (string.IsNullOrEmpty(plain))
            {
                skipReason = "empty plain text";
                return false;
            }

            if (plain.Contains("[译]"))
            {
                skipReason = "already translated";
                return false;
            }

            if (QuickInfoDocumentationExtractor.IsLikelySignatureLine(plain))
            {
                skipReason = "likely signature line: " + SignatureHelpDiagnostics.Truncate(plain, 60);
                return false;
            }

            if (plain.Length < 12)
            {
                skipReason = "text too short, len=" + plain.Length;
                return false;
            }

            return true;
        }

        private TView CreateInnerElement<TView>(ITextView textView, object model) where TView : class
        {
            if (ViewElementFactories == null)
            {
                SignatureHelpDiagnostics.Log("Factory: ViewElementFactories is null, using fallback");
                return CreateFallbackElement<TView>(model);
            }

            IList<Lazy<IViewElementFactory, IOrderable>> ordered = Orderer.Order(ViewElementFactories);
            bool afterSelf = false;
            int tried = 0;
            foreach (Lazy<IViewElementFactory, IOrderable> lazy in ordered)
            {
                if (!afterSelf)
                {
                    if (ReferenceEquals(lazy.Value, this))
                    {
                        afterSelf = true;
                    }

                    continue;
                }

                tried++;
                TView result = lazy.Value.CreateViewElement<TView>(textView, model);
                if (result != null)
                {
                    SignatureHelpDiagnostics.LogFormat(
                        "Factory: inner from {0} ({1})",
                        lazy.Metadata?.Name ?? "(no name)",
                        lazy.Value.GetType().Name);
                    return result;
                }
            }

            SignatureHelpDiagnostics.LogFormat("Factory: no inner factory matched after {0} tries", tried);
            return CreateFallbackElement<TView>(model);
        }

        private static TView CreateFallbackElement<TView>(object model) where TView : class
        {
            if (!IsUiElementView<TView>())
            {
                return null;
            }

            string text = AdornmentPlainTextExtractor.TryExtractPlainText(model) ?? string.Empty;
            return new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap
            } as TView;
        }
    }
}
