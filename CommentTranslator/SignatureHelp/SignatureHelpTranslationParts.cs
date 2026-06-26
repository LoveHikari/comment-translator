using System.Text;
using CommentTranslator.QuickInfo;
using Microsoft.VisualStudio.Language.Intellisense;

namespace CommentTranslator.SignatureHelp
{
    internal sealed class SignatureHelpTranslationParts
    {
        internal string SignaturePlain { get; private set; }
        internal string ParameterName { get; private set; }
        internal string ParameterPlain { get; private set; }

        internal string CacheKey =>
            (SignaturePlain ?? string.Empty) + "\0"
            + (ParameterName ?? string.Empty) + "\0"
            + (ParameterPlain ?? string.Empty);

        internal bool IsEmpty =>
            string.IsNullOrWhiteSpace(SignaturePlain) && string.IsNullOrWhiteSpace(ParameterPlain);

        internal static SignatureHelpTranslationParts FromSignature(ISignature signature)
        {
            var parts = new SignatureHelpTranslationParts();
            if (signature == null)
            {
                return parts;
            }

            if (!string.IsNullOrWhiteSpace(signature.Documentation))
            {
                parts.SignaturePlain = signature.Documentation.Trim();
            }
            else
            {
                string fromContent = QuickInfoDocumentationExtractor.TryExtractFromSignatureHelpText(signature.Content);
                if (!string.IsNullOrWhiteSpace(fromContent))
                {
                    parts.SignaturePlain = fromContent.Trim();
                }
            }

            IParameter parameter = signature.CurrentParameter;
            if (parameter == null && signature.Parameters != null)
            {
                foreach (IParameter candidate in signature.Parameters)
                {
                    if (!string.IsNullOrWhiteSpace(candidate?.Documentation))
                    {
                        parameter = candidate;
                        break;
                    }
                }
            }

            if (parameter == null || string.IsNullOrWhiteSpace(parameter.Documentation))
            {
                return parts;
            }

            parts.ParameterName = string.IsNullOrWhiteSpace(parameter.Name) ? "parameter" : parameter.Name.Trim();
            parts.ParameterPlain = parameter.Documentation.Trim();
            return parts;
        }
    }
}
