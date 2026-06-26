using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommentTranslator.IntelliSense;
using CommentTranslator.QuickInfo;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace CommentTranslator.SignatureHelp
{
    /// <summary>
    /// 在签名帮助 Interactive 视图中监听文本变化，用 adornment 层在文档下方显示 [译]。
    /// </summary>
    internal sealed class SignatureHelpDocTranslationController
    {
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private SignatureHelpDocTranslationAdornment _adornment;
        private string _lastSourceText;
        private int _translateGeneration;
        private int _scheduleGeneration;

        internal static void Attach(IWpfTextView view)
        {
            view.Properties.GetOrCreateSingletonProperty(
                () => new SignatureHelpDocTranslationController(view));
        }

        private SignatureHelpDocTranslationController(IWpfTextView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _layer = view.GetAdornmentLayer(SignatureHelpDocTranslationConnector.LayerName);
            SignatureHelpDiagnostics.LogTextView(view, "Controller.Attached");
            _view.TextBuffer.Changed += OnViewBufferChanged;
            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnViewClosed;
            ScheduleTranslate();
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            _layer.RemoveAllAdornments();
        }

        private void OnViewBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ScheduleTranslate();
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            UpdatePlacement();
        }

        private void ScheduleTranslate()
        {
            int generation = Interlocked.Increment(ref _scheduleGeneration);
            _ = Task.Run(async () =>
            {
                await Task.Delay(80).ConfigureAwait(false);
                if (generation != _scheduleGeneration)
                {
                    return;
                }

                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    if (generation == _scheduleGeneration)
                    {
                        TranslateNow();
                    }
                }));
            });
        }

        private void TranslateNow()
        {
            if (!SignatureHelpDocTranslationHelper.IsEnabled())
            {
                SignatureHelpDiagnostics.Log("Controller: disabled, removing adornment");
                RemoveAdornment();
                return;
            }

            string fullText = _view.TextBuffer.CurrentSnapshot.GetText();
            if (string.IsNullOrWhiteSpace(fullText))
            {
                SignatureHelpDiagnostics.Log("Controller: buffer empty, keep existing adornment");
                UpdatePlacement();
                return;
            }

            string source = QuickInfoDocumentationExtractor.TryExtractFromSignatureHelpText(fullText);
            if (string.IsNullOrEmpty(source))
            {
                SignatureHelpDiagnostics.LogFormat(
                    "Controller: no doc extracted, bufferPreview={0}",
                    SignatureHelpDiagnostics.Truncate(fullText));
                UpdatePlacement();
                return;
            }

            if (string.Equals(source, _lastSourceText, StringComparison.Ordinal))
            {
                UpdatePlacement();
                return;
            }

            SignatureHelpDiagnostics.LogFormat(
                "Controller: translating source={0}",
                SignatureHelpDiagnostics.Truncate(source));
            _lastSourceText = source;
            string plain = source;
            IntelliSenseTranslationHelper.PretreatPlainText(ref plain);
            if (string.IsNullOrEmpty(plain))
            {
                SignatureHelpDiagnostics.Log("Controller: pretreat yielded empty");
                RemoveAdornment();
                return;
            }

            string cached = IntelliSenseTranslationHelper.TryGetCachedTranslation(plain);
            if (!string.IsNullOrEmpty(cached))
            {
                SignatureHelpDiagnostics.LogFormat(
                    "Controller: cache hit, len={0}",
                    cached.Length);
                ShowTranslation(cached);
                return;
            }

            SignatureHelpDiagnostics.Log("Controller: requesting translation async");
            int generation = Interlocked.Increment(ref _translateGeneration);
            _ = Task.Run(async () =>
            {
                try
                {
                    string translated = await IntelliSenseTranslationHelper
                        .GetOrTranslateAsync(plain, CancellationToken.None)
                        .ConfigureAwait(false);
                    if (generation != _translateGeneration || string.IsNullOrEmpty(translated))
                    {
                        SignatureHelpDiagnostics.LogFormat(
                            "Controller: async skipped gen={0}/{1}, empty={2}",
                            generation,
                            _translateGeneration,
                            string.IsNullOrEmpty(translated));
                        return;
                    }

                    SignatureHelpDiagnostics.LogFormat(
                        "Controller: async ok, len={0}",
                        translated.Length);
                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        if (generation == _translateGeneration)
                        {
                            ShowTranslation(translated);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    SignatureHelpDiagnostics.Log("Controller: async error — " + ex.Message);
                }
            });
        }

        private void ShowTranslation(string translatedPlainText)
        {
            if (_adornment == null)
            {
                _adornment = new SignatureHelpDocTranslationAdornment(translatedPlainText);
            }
            else
            {
                _adornment.Update(translatedPlainText);
            }

            _adornment.MaxWidth = Math.Max(0, _view.ViewportWidth - 12);
            UpdatePlacement();
        }

        private void UpdatePlacement()
        {
            if (_adornment == null)
            {
                return;
            }

            ITextViewLine lastLine = _view.TextViewLines?.LastVisibleLine;
            if (lastLine == null)
            {
                SignatureHelpDiagnostics.Log("Controller: UpdatePlacement skipped — no visible lines");
                return;
            }

            _layer.RemoveAllAdornments();
            double top = lastLine.Bottom - _view.ViewportTop + 4;
            _layer.AddAdornment(
                AdornmentPositioningBehavior.ViewportRelative,
                null,
                null,
                _adornment,
                (tag, element) =>
                {
                    Canvas.SetTop(element, top);
                    Canvas.SetLeft(element, 4);
                });
        }

        private void RemoveAdornment()
        {
            _lastSourceText = null;
            _adornment = null;
            _layer.RemoveAllAdornments();
        }
    }
}
