using System.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace CommentTranslator.IntelliSense
{
    /// <summary>
    /// 从编辑器气泡/补全说明使用的 <see cref="ContainerElement"/> 等结构中抽取纯文本，
    /// 供翻译服务使用（不包含样式信息）。
    /// </summary>
    internal static class AdornmentPlainTextExtractor
    {
        /// <summary>
        /// 将 Quick Info 或补全说明对象转换为可发送给翻译 API 的纯文本。
        /// </summary>
        /// <param name="description">由语言服务返回的说明对象（常见为 string 或 ContainerElement）。</param>
        /// <returns>抽取后的文本；无法识别时返回 null。</returns>
        public static string TryExtractPlainText(object description)
        {
            if (description == null)
            {
                return null;
            }

            if (description is string s)
            {
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }

            if (description is ContainerElement container)
            {
                var sb = new StringBuilder();
                AppendContainer(container, sb);
                var text = sb.ToString().Trim();
                return string.IsNullOrEmpty(text) ? null : text;
            }

            if (description is ClassifiedTextElement classified)
            {
                var sb = new StringBuilder();
                AppendClassified(classified, sb);
                var text = sb.ToString().Trim();
                return string.IsNullOrEmpty(text) ? null : text;
            }

            return null;
        }

        private static void AppendContainer(ContainerElement container, StringBuilder sb)
        {
            foreach (object element in container.Elements)
            {
                switch (element)
                {
                    case ClassifiedTextElement c:
                        AppendClassified(c, sb);
                        break;
                    case ContainerElement nested:
                        AppendContainer(nested, sb);
                        break;
                    case string str:
                        if (!string.IsNullOrEmpty(str))
                        {
                            sb.Append(str);
                        }

                        break;
                }
            }
        }

        private static void AppendClassified(ClassifiedTextElement classified, StringBuilder sb)
        {
            if (classified.Runs == null)
            {
                return;
            }

            foreach (var run in classified.Runs)
            {
                if (!string.IsNullOrEmpty(run.Text))
                {
                    sb.Append(run.Text);
                }
            }
        }
    }
}
