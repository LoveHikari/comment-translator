using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommentTranslator.QuickInfo;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CommentTranslator.SignatureHelp
{
    /// <summary>
    /// 在 C# 编辑器视图上监听签名帮助会话（不依赖 sighelp-doc content type）。
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class SignatureHelpEditorSessionMonitor : IWpfTextViewCreationListener
    {
        [Import]
        internal ISignatureHelpBroker SignatureHelpBroker { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            SignatureHelpDiagnostics.LogTextView(textView, "EditorMonitor.Attached");
            SignatureHelpEditorSessionMonitorCore.Attach(textView, SignatureHelpBroker);
        }
    }

    internal sealed class SignatureHelpEditorSessionMonitorCore
    {
        private readonly IWpfTextView _view;
        private readonly ISignatureHelpBroker _broker;
        private bool _wasActive;
        private SignatureHelpSessionTranslationHost _translationHost;

        internal static void Attach(IWpfTextView view, ISignatureHelpBroker broker)
        {
            if (view == null || broker == null)
            {
                SignatureHelpDiagnostics.Log("EditorMonitor: view or broker is null");
                return;
            }

            view.Properties.GetOrCreateSingletonProperty(
                () => new SignatureHelpEditorSessionMonitorCore(view, broker));
        }

        private SignatureHelpEditorSessionMonitorCore(IWpfTextView view, ISignatureHelpBroker broker)
        {
            _view = view;
            _broker = broker;
            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnClosed;
            SignatureHelpDiagnostics.Log("EditorMonitor: listening on editor view");
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnClosed;
            DetachTranslationHost();
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            bool active;
            try
            {
                active = _broker.IsSignatureHelpActive(_view);
            }
            catch (Exception ex)
            {
                SignatureHelpDiagnostics.Log("EditorMonitor: IsSignatureHelpActive error — " + ex.Message);
                return;
            }

            if (active == _wasActive)
            {
                return;
            }

            _wasActive = active;
            SignatureHelpDiagnostics.LogFormat("EditorMonitor: SignatureHelp active={0}", active);
            if (!active)
            {
                DetachTranslationHost();
                return;
            }

            EnsureTranslationHost();
            LogSessions();
        }

        private void EnsureTranslationHost()
        {
            if (_translationHost != null)
            {
                return;
            }

            ISignatureHelpSession session = _broker.GetSessions(_view)?.FirstOrDefault();
            if (session == null)
            {
                SignatureHelpDiagnostics.Log("EditorMonitor: no session to attach host");
                return;
            }

            _translationHost = SignatureHelpSessionTranslationHost.TryAttachForSession(
                session,
                _view,
                () => _translationHost = null);
        }

        private void DetachTranslationHost()
        {
            if (_translationHost == null)
            {
                return;
            }

            _translationHost.Detach();
            _translationHost = null;
        }

        private void LogSessions()
        {
            try
            {
                var sessions = _broker.GetSessions(_view);
                int count = sessions?.Count() ?? 0;
                SignatureHelpDiagnostics.LogFormat("EditorMonitor: session count={0}", count);
                if (sessions == null)
                {
                    return;
                }

                foreach (ISignatureHelpSession session in sessions)
                {
                    SignatureHelpDiagnostics.LogFormat(
                        "EditorMonitor: session type={0}, signatures={1}",
                        session?.GetType().FullName ?? "(null)",
                        session?.Signatures?.Count ?? 0);

                    if (session?.SelectedSignature != null)
                    {
                        LogSignature("selected", session.SelectedSignature);
                    }
                    else if (session?.Signatures != null && session.Signatures.Count > 0)
                    {
                        LogSignature("first", session.Signatures[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                SignatureHelpDiagnostics.Log("EditorMonitor: LogSessions error — " + ex);
            }
        }

        private static void LogSignature(string label, ISignature signature)
        {
            string doc = signature?.Documentation;
            string content = signature?.Content;
            string extracted = QuickInfoDocumentationExtractor.TryExtractFromSignatureHelpText(content);
            SignatureHelpDiagnostics.LogFormat(
                "{0} signature: doc={1}, content={2}, extracted={3}",
                label,
                SignatureHelpDiagnostics.Truncate(doc),
                SignatureHelpDiagnostics.Truncate(content, 80),
                SignatureHelpDiagnostics.Truncate(extracted));
        }

        private void LogViewProperties()
        {
            try
            {
                if (_view.Properties?.PropertyList == null)
                {
                    return;
                }

                SignatureHelpDiagnostics.LogFormat(
                    "EditorMonitor: view property count={0}",
                    _view.Properties.PropertyList.Count);
            }
            catch (Exception ex)
            {
                SignatureHelpDiagnostics.Log("EditorMonitor: LogViewProperties error — " + ex.Message);
            }
        }
    }
}
