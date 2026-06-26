using System.Collections.Generic;
using System.ComponentModel.Composition;
using CommentTranslator.IntelliSense;
using CommentTranslator.QuickInfo;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace CommentTranslator.SignatureHelp
{
    /// <summary>
    /// 在签名帮助会话建立时预取文档译文到缓存，供 <see cref="SignatureHelpViewElementFactory"/> 同步读取。
    /// 不修改 <see cref="ISignature"/> 实例，不调用 <see cref="ISignatureHelpSession.Recalculate"/>。
    /// </summary>
    [Export(typeof(ISignatureHelpSourceProvider))]
    [Name("Comment Translator Signature Help Documentation Prefetch")]
    [ContentType("text")]
    [ContentType("code")]
    [ContentType("projection")]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    [Order(After = "default")]
    internal sealed class SignatureHelpDocumentationPrefetchSourceProvider : ISignatureHelpSourceProvider
    {
        public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer)
        {
            SignatureHelpDiagnostics.LogFormat(
                "PrefetchProvider: TryCreate for buffer contentType={0}",
                textBuffer?.ContentType?.TypeName ?? "(null)");
            return new SignatureHelpDocumentationPrefetchSource();
        }
    }

    internal sealed class SignatureHelpDocumentationPrefetchSource : ISignatureHelpSource
    {
        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            SignatureHelpDiagnostics.LogFormat(
                "Prefetch: Augment called, signatures={0}, enabled={1}",
                signatures?.Count ?? 0,
                SignatureHelpDocTranslationHelper.IsEnabled());

            if (!SignatureHelpDocTranslationHelper.IsEnabled() || signatures == null)
            {
                SignatureHelpDiagnostics.LogEnabledState();
                return;
            }

            foreach (ISignature signature in signatures)
            {
                PrefetchFromTarget(signature, "signature");
                if (signature?.Parameters == null)
                {
                    continue;
                }

                foreach (IParameter parameter in signature.Parameters)
                {
                    PrefetchFromTarget(parameter, "parameter");
                }
            }

            SignatureHelpSessionHelper.EnsureTranslationHost(session);
        }

        public ISignature GetBestMatch(ISignatureHelpSession session)
        {
            return null;
        }

        public void Dispose()
        {
        }

        private static void PrefetchFromTarget(object target, string kind)
        {
            string original = ReadDocumentationText(target);
            if (string.IsNullOrWhiteSpace(original))
            {
                SignatureHelpDiagnostics.LogFormat("Prefetch: {0} has no documentation", kind);
                return;
            }

            SignatureHelpDiagnostics.LogFormat(
                "Prefetch: {0} doc={1}",
                kind,
                SignatureHelpDiagnostics.Truncate(original));

            string plain = original;
            IntelliSenseTranslationHelper.PretreatPlainText(ref plain);
            if (!string.IsNullOrEmpty(plain))
            {
                IntelliSenseTranslationHelper.PrefetchTranslate(plain);
            }
        }

        private static string ReadDocumentationText(object target)
        {
            if (target is ISignature signature)
            {
                if (!string.IsNullOrWhiteSpace(signature.Documentation))
                {
                    return signature.Documentation;
                }

                return QuickInfoDocumentationExtractor.TryExtractFromSignatureHelpText(signature.Content);
            }

            if (target is IParameter parameter)
            {
                return parameter.Documentation;
            }

            return null;
        }
    }
}
