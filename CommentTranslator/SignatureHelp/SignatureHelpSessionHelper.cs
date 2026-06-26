using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace CommentTranslator.SignatureHelp
{
    internal static class SignatureHelpSessionHelper
    {
        private static readonly object s_hostPropertyKey = typeof(SignatureHelpSessionTranslationHost);

        internal static void EnsureTranslationHost(ISignatureHelpSession session)
        {
            if (session == null || !SignatureHelpDocTranslationHelper.IsEnabled())
            {
                return;
            }

            IWpfTextView editorView = TryGetWpfTextView(session);
            SignatureHelpDiagnostics.LogFormat(
                "SessionHelper: EnsureTranslationHost editorView={0}",
                editorView != null);

            SignatureHelpSessionTranslationHost host = session.Properties.GetOrCreateSingletonProperty(
                s_hostPropertyKey,
                () => SignatureHelpSessionTranslationHost.TryAttachForSession(
                    session,
                    editorView,
                    () => session.Properties.RemoveProperty(s_hostPropertyKey)));

            host?.Refresh();
        }

        internal static IWpfTextView TryGetWpfTextView(ISignatureHelpSession session)
        {
            if (session?.Properties?.PropertyList == null)
            {
                return null;
            }

            for (int i = 0; i < session.Properties.PropertyList.Count; i++)
            {
                object value = session.Properties.PropertyList[i].Value;
                if (value is IWpfTextView wpfTextView)
                {
                    return wpfTextView;
                }

                if (value is ITextView textView && textView is IWpfTextView wpfFromTextView)
                {
                    return wpfFromTextView;
                }
            }

            return null;
        }
    }
}
