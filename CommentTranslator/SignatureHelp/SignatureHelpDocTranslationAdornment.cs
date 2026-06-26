using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CommentTranslator.SignatureHelp
{
    /// <summary>
    /// 签名帮助面板最下方的 [译] 译文块（方法说明 + 参数说明分区展示）。
    /// </summary>
    internal sealed class SignatureHelpDocTranslationAdornment : Border
    {
        internal int TranslateGeneration;

        private readonly TextBlock _prefixBlock;
        private readonly TextBlock _signatureBlock;
        private readonly TextBlock _parameterBlock;

        public SignatureHelpDocTranslationAdornment(string translatedPlainText)
        {
            Background = Brushes.Transparent;
            BorderBrush = new SolidColorBrush(Color.FromArgb(64, 128, 128, 128));
            BorderThickness = new Thickness(0, 1, 0, 0);
            Padding = new Thickness(0, 8, 0, 2);
            Margin = new Thickness(0, 4, 0, 0);

            _prefixBlock = new TextBlock
            {
                Text = "[译]",
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(86, 156, 214)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            _signatureBlock = CreateBodyBlock();
            _parameterBlock = CreateBodyBlock();

            var stack = new StackPanel();
            stack.Children.Add(_prefixBlock);
            stack.Children.Add(_signatureBlock);
            stack.Children.Add(_parameterBlock);
            Child = stack;

            Update(translatedPlainText, null, null);
        }

        public void Update(string signatureTranslation, string parameterName, string parameterTranslation)
        {
            bool hasSignature = !string.IsNullOrWhiteSpace(signatureTranslation);
            bool hasParameter = !string.IsNullOrWhiteSpace(parameterTranslation);

            Visibility = hasSignature || hasParameter ? Visibility.Visible : Visibility.Collapsed;
            _prefixBlock.Visibility = hasSignature || hasParameter ? Visibility.Visible : Visibility.Collapsed;

            _signatureBlock.Text = signatureTranslation ?? string.Empty;
            _signatureBlock.Visibility = hasSignature ? Visibility.Visible : Visibility.Collapsed;

            if (hasParameter)
            {
                _parameterBlock.Text = (parameterName ?? "parameter") + ": " + parameterTranslation;
                _parameterBlock.Visibility = Visibility.Visible;
            }
            else
            {
                _parameterBlock.Text = string.Empty;
                _parameterBlock.Visibility = Visibility.Collapsed;
            }
        }

        public void Update(string composedPlainText)
        {
            if (string.IsNullOrWhiteSpace(composedPlainText))
            {
                Update(null, null, null);
                return;
            }

            string[] lines = composedPlainText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                Update(null, null, null);
                return;
            }

            if (lines.Length == 1)
            {
                if (lines[0].Contains(": "))
                {
                    int split = lines[0].IndexOf(": ", System.StringComparison.Ordinal);
                    Update(null, lines[0].Substring(0, split), lines[0].Substring(split + 2));
                }
                else
                {
                    Update(lines[0], null, null);
                }

                return;
            }

            string lastLine = lines[lines.Length - 1];
            int paramSplit = lastLine.IndexOf(": ", System.StringComparison.Ordinal);
            if (paramSplit > 0)
            {
                var signatureBuilder = new System.Text.StringBuilder();
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    if (signatureBuilder.Length > 0)
                    {
                        signatureBuilder.AppendLine();
                    }

                    signatureBuilder.Append(lines[i]);
                }

                Update(
                    signatureBuilder.Length > 0 ? signatureBuilder.ToString() : null,
                    lastLine.Substring(0, paramSplit),
                    lastLine.Substring(paramSplit + 2));
            }
            else
            {
                Update(composedPlainText, null, null);
            }
        }

        private static TextBlock CreateBodyBlock()
        {
            return new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(106, 153, 85)),
                Margin = new Thickness(0, 0, 0, 4)
            };
        }
    }
}
