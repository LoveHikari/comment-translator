using System.Threading;
using System.Threading.Tasks;
using CommentTranslator.IntelliSense;

namespace CommentTranslator.SignatureHelp
{
    internal sealed class SignatureHelpTranslatedContent
    {
        internal string SignatureText { get; set; }
        internal string ParameterName { get; set; }
        internal string ParameterText { get; set; }

        internal bool IsEmpty =>
            string.IsNullOrWhiteSpace(SignatureText) && string.IsNullOrWhiteSpace(ParameterText);
    }

    internal static class SignatureHelpTranslationComposer
    {
        internal static async Task<SignatureHelpTranslatedContent> BuildTranslatedContentAsync(
            SignatureHelpTranslationParts parts,
            CancellationToken cancellationToken = default)
        {
            if (parts == null || parts.IsEmpty)
            {
                return null;
            }

            var result = new SignatureHelpTranslatedContent();
            result.SignatureText = await TranslatePartAsync(parts.SignaturePlain, cancellationToken).ConfigureAwait(false);
            result.ParameterName = parts.ParameterName;
            result.ParameterText = await TranslatePartAsync(parts.ParameterPlain, cancellationToken).ConfigureAwait(false);
            return result.IsEmpty ? null : result;
        }

        internal static SignatureHelpTranslatedContent TryBuildFromCache(SignatureHelpTranslationParts parts)
        {
            if (parts == null || parts.IsEmpty)
            {
                return null;
            }

            var result = new SignatureHelpTranslatedContent
            {
                SignatureText = TryGetCachedTranslation(parts.SignaturePlain),
                ParameterName = parts.ParameterName,
                ParameterText = TryGetCachedTranslation(parts.ParameterPlain)
            };

            return result.IsEmpty ? null : result;
        }

        internal static bool HasPendingTranslation(SignatureHelpTranslationParts parts)
        {
            if (parts == null || parts.IsEmpty)
            {
                return false;
            }

            return IsPending(parts.SignaturePlain) || IsPending(parts.ParameterPlain);
        }

        private static bool IsPending(string plain)
        {
            if (string.IsNullOrWhiteSpace(plain))
            {
                return false;
            }

            string key = plain;
            IntelliSenseTranslationHelper.PretreatPlainText(ref key);
            return !string.IsNullOrEmpty(key)
                && IntelliSenseTranslationHelper.TryGetCachedTranslation(key) == null;
        }

        private static string TryGetCachedTranslation(string plain)
        {
            if (string.IsNullOrWhiteSpace(plain))
            {
                return null;
            }

            string key = plain;
            IntelliSenseTranslationHelper.PretreatPlainText(ref key);
            return string.IsNullOrEmpty(key) ? null : IntelliSenseTranslationHelper.TryGetCachedTranslation(key);
        }

        private static async Task<string> TranslatePartAsync(string plain, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(plain))
            {
                return null;
            }

            string key = plain;
            IntelliSenseTranslationHelper.PretreatPlainText(ref key);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return await IntelliSenseTranslationHelper.GetOrTranslateAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }
}
