using System;
using System.ComponentModel.Composition;
using CommentTranslator.QuickInfo;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CommentTranslator.SignatureHelp
{
    /// <summary>
    /// 在签名帮助文档视图创建时挂载文档翻译控制器（文本缓冲区路径，兼容旧版 VS）。
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("sighelp-doc")]
    [ContentType("sighelp")]
    [ContentType("sighelp-documentation")]
    [ContentType("intellisense")]
    internal sealed class SignatureHelpDocTranslationConnector : IWpfTextViewCreationListener
    {
        static SignatureHelpDocTranslationConnector()
        {
            SignatureHelpDiagnostics.Log("SignatureHelpDocTranslationConnector MEF part loaded");
        }

        internal const string LayerName = "SignatureHelpDocTranslation";

#pragma warning disable 649, 169
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(LayerName)]
        [Order(After = PredefinedAdornmentLayers.Text)]
        [Order(After = PredefinedAdornmentLayers.Squiggle)]
        private AdornmentLayerDefinition _layerDefinition;
#pragma warning restore 649, 169

        public void TextViewCreated(IWpfTextView textView)
        {
            SignatureHelpDiagnostics.LogTextView(textView, "Connector.TextViewCreated");
            SignatureHelpDiagnostics.LogEnabledState();

            if (!ShouldAttachToView(textView, out string rejectReason))
            {
                SignatureHelpDiagnostics.Log("Connector: not attached — " + rejectReason);
                return;
            }

            SignatureHelpDiagnostics.Log("Connector: attaching controller");
            SignatureHelpDocTranslationController.Attach(textView);
        }

        private static bool ShouldAttachToView(IWpfTextView textView, out string rejectReason)
        {
            rejectReason = null;
            string contentType = textView.TextBuffer.ContentType.TypeName;
            if (string.Equals(contentType, "sighelp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contentType, "sighelp-doc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contentType, "sighelp-documentation", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.Equals(contentType, "intellisense", StringComparison.OrdinalIgnoreCase))
            {
                rejectReason = "contentType not sighelp/intellisense: " + contentType;
                return false;
            }

            string text = textView.TextBuffer.CurrentSnapshot.GetText();
            if (string.IsNullOrWhiteSpace(text) || text.Length > 8000)
            {
                rejectReason = "intellisense buffer empty or too large, len=" + (text?.Length ?? 0);
                return false;
            }

            bool hasSignatureLine = false;
            bool hasDocLine = false;
            foreach (string line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (QuickInfoDocumentationExtractor.IsLikelySignatureLine(trimmed))
                {
                    hasSignatureLine = true;
                }
                else if (trimmed.Length > 10)
                {
                    hasDocLine = true;
                }
            }

            if (!hasSignatureLine || !hasDocLine)
            {
                rejectReason = "intellisense heuristic failed: hasSignatureLine="
                    + hasSignatureLine + ", hasDocLine=" + hasDocLine
                    + ", preview=" + SignatureHelpDiagnostics.Truncate(text, 80);
                return false;
            }

            return true;
        }
    }

    internal static class SignatureHelpDocTranslationHelper
    {
        internal static bool IsEnabled()
        {
            return CommentTranslatorPackage.Settings != null
                && CommentTranslatorPackage.Settings.AutoTranslateSignatureHelp
                && CommentTranslatorPackage.TranslateClient != null;
        }
    }
}
