using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace CommentTranslator.SignatureHelp
{
    /// <summary>
    /// 在完整签名帮助气泡下方用独立 Popup 显示 [译]，不修改气泡内部布局（避免固定高度导致遮挡原文）。
    /// </summary>
    internal sealed class SignatureHelpSessionTranslationHost
    {
        private readonly ISignatureHelpSession _session;
        private readonly IWpfTextView _editorView;
        private readonly Action _onDetached;
        private Popup _popup;
        private FrameworkElement _anchorElement;
        private Border _popupContainer;
        private SignatureHelpDocTranslationAdornment _translationBlock;
        private string _lastSourceText;
        private int _translateGeneration;
        private int _attachAttempt;

        internal static SignatureHelpSessionTranslationHost TryAttachForSession(
            ISignatureHelpSession session,
            IWpfTextView editorView,
            Action onDetached)
        {
            if (session == null || !SignatureHelpDocTranslationHelper.IsEnabled())
            {
                return null;
            }

            var host = new SignatureHelpSessionTranslationHost(session, editorView, onDetached);
            host.BeginAttach();
            return host;
        }

        private SignatureHelpSessionTranslationHost(
            ISignatureHelpSession session,
            IWpfTextView editorView,
            Action onDetached)
        {
            _session = session;
            _editorView = editorView;
            _onDetached = onDetached;
            _session.SelectedSignatureChanged += OnSelectedSignatureChanged;
            _session.Dismissed += OnSessionDismissed;
            if (_editorView != null)
            {
                _editorView.Caret.PositionChanged += OnCaretPositionChanged;
            }
        }

        internal void Refresh()
        {
            BeginAttach();
            ScheduleTranslate();
        }

        internal void Detach()
        {
            _session.SelectedSignatureChanged -= OnSelectedSignatureChanged;
            _session.Dismissed -= OnSessionDismissed;
            if (_editorView != null)
            {
                _editorView.Caret.PositionChanged -= OnCaretPositionChanged;
            }

            CloseTranslationUi();
            SignatureHelpDiagnostics.Log("SessionHost: detached");
            _onDetached?.Invoke();
        }

        private void BeginAttach()
        {
            SchedulePresenterAttach();
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            Detach();
        }

        private void OnSelectedSignatureChanged(object sender, EventArgs e)
        {
            SchedulePresenterAttach();
            ScheduleTranslate();
        }

        private void OnCaretPositionChanged(object sender, EventArgs e)
        {
            ScheduleTranslate();
        }

        private void OnAnchorLayoutUpdated(object sender, EventArgs e)
        {
            if (_popup != null && _popup.IsOpen && _anchorElement != null)
            {
                _popup.UpdateLayout();
            }
        }

        private void SchedulePresenterAttach()
        {
            int attempt = Interlocked.Increment(ref _attachAttempt);
            _ = Task.Run(async () =>
            {
                for (int i = 0; i < 40; i++)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                    if (attempt != _attachAttempt)
                    {
                        return;
                    }

                    bool attached = false;
                    Application.Current?.Dispatcher?.Invoke(new Action(() =>
                    {
                        attached = TryEnsureTranslationUi();
                    }));

                    if (attached)
                    {
                        SignatureHelpDiagnostics.LogFormat("SessionHost: popup attached on try {0}", i + 1);
                        return;
                    }
                }

                SignatureHelpDiagnostics.Log("SessionHost: popup attach failed after retries");
            });
        }

        private bool TryEnsureTranslationUi()
        {
            if (_translationBlock == null)
            {
                _translationBlock = new SignatureHelpDocTranslationAdornment(null);
            }

            FrameworkElement presenter = SignatureHelpVisualFinder.FindPresenterSurface(_editorView);
            FrameworkElement anchor = SignatureHelpVisualFinder.FindSignatureHelpTooltipRoot(presenter);
            if (anchor == null)
            {
                SignatureHelpDiagnostics.Log("SessionHost: tooltip root not found");
                return false;
            }

            anchor.UpdateLayout();
            if (anchor.ActualHeight <= 0 || anchor.ActualWidth <= 0)
            {
                return false;
            }

            if (_anchorElement != anchor)
            {
                if (_anchorElement != null)
                {
                    _anchorElement.LayoutUpdated -= OnAnchorLayoutUpdated;
                }

                _anchorElement = anchor;
                _anchorElement.LayoutUpdated += OnAnchorLayoutUpdated;
            }

            if (_popup == null)
            {
                _popupContainer = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 6, 8, 8),
                    Child = _translationBlock
                };

                _popup = new Popup
                {
                    Child = _popupContainer,
                    Placement = PlacementMode.Custom,
                    CustomPopupPlacementCallback = SignatureHelpVisualFinder.PlaceBelowTarget,
                    StaysOpen = true,
                    AllowsTransparency = false,
                    PopupAnimation = PopupAnimation.None
                };
            }

            _popup.PlacementTarget = anchor;
            _translationBlock.MaxWidth = Math.Max(280, SignatureHelpVisualFinder.GetContentWidth(anchor) - 16);
            _popup.IsOpen = true;

            SignatureHelpDiagnostics.LogFormat(
                "SessionHost: popup below {0}, size={1:F0}x{2:F0}",
                anchor.GetType().Name,
                anchor.ActualWidth,
                anchor.ActualHeight);

            return true;
        }

        private void ScheduleTranslate()
        {
            if (!SignatureHelpDocTranslationHelper.IsEnabled())
            {
                return;
            }

            SignatureHelpTranslationParts parts = SignatureHelpTranslationParts.FromSignature(_session.SelectedSignature);
            if (parts.IsEmpty)
            {
                return;
            }

            string cacheKey = parts.CacheKey;
            if (string.Equals(cacheKey, _lastSourceText, StringComparison.Ordinal))
            {
                return;
            }

            _lastSourceText = cacheKey;

            SignatureHelpTranslatedContent cached = SignatureHelpTranslationComposer.TryBuildFromCache(parts);
            if (cached != null && !cached.IsEmpty)
            {
                ShowTranslation(cached);
                if (!SignatureHelpTranslationComposer.HasPendingTranslation(parts))
                {
                    return;
                }
            }

            int generation = Interlocked.Increment(ref _translateGeneration);
            _ = Task.Run(async () =>
            {
                try
                {
                    SignatureHelpTranslatedContent translated = await SignatureHelpTranslationComposer
                        .BuildTranslatedContentAsync(parts, CancellationToken.None)
                        .ConfigureAwait(false);
                    if (generation != _translateGeneration || translated == null || translated.IsEmpty)
                    {
                        return;
                    }

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
                    SignatureHelpDiagnostics.Log("SessionHost: translate error — " + ex.Message);
                }
            });
        }

        private void ShowTranslation(SignatureHelpTranslatedContent content)
        {
            TryEnsureTranslationUi();
            if (_translationBlock == null || content == null)
            {
                return;
            }

            _translationBlock.Update(content.SignatureText, content.ParameterName, content.ParameterText);
        }

        private void CloseTranslationUi()
        {
            if (_anchorElement != null)
            {
                _anchorElement.LayoutUpdated -= OnAnchorLayoutUpdated;
                _anchorElement = null;
            }

            if (_popup != null)
            {
                _popup.IsOpen = false;
                _popup = null;
            }

            _popupContainer = null;
            _translationBlock = null;
            _lastSourceText = null;
        }
    }
}
