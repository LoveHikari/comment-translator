using Framework;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentTranslator.QuickInfo.comment
{
    internal class CommentTranslate
    {
        /// <summary>
        /// 尝试翻译方法或变量的注释文本
        /// </summary>
        /// <param name="session"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ClassifiedTextRun>> TryTranslateMethodInformationAsync(IAsyncQuickInfoSession session, string typeName)
        {
            var str = TryGetMethodInformation(session, typeName);
            if (str != null)
            {
                StringPretreatment(ref str);

                var recv = await CommentTranslatorPackage.TranslateClient.TranslateAsync(str);

                var temp = new List<ApiResponse> { recv };
                CreateClassifiedTextRun(temp, out var runs);
                return runs;

            }
            return null;
        }

        /// <summary>
        /// 尝试从 IAsyncQuickInfoSession 中获取鼠标指向的方法或变量的注解
        /// </summary>
        /// <param name="session"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private static string TryGetMethodInformation(IAsyncQuickInfoSession session, string typeName)
        {
            if (session.Properties.PropertyList.Count > 0)
            {
                // 这个 Value 的类型应该为 Microsoft.VisualStudio.Language.Intellisense.Implementation.LegacyQuickInfoSession
                // 但是我还没有找到这个类型所在的命名空间，现在使用的是类型尝试转换，如果转换不成会返回 null
                var quick = session.Properties.PropertyList[0].Value as IQuickInfoSession;
                foreach (ContainerElement i in quick.QuickInfoContent.Cast<ContainerElement>())
                {
                    if (typeName == "C/C++")
                    {
                        return TryGetCppMethodInformation(i.Elements);
                    }
                    foreach (ContainerElement element in i.Elements.Cast<ContainerElement>())
                    {
                        if (element.Elements.Count() < 2)
                        {
                            return null;
                        }
                        if (element.Elements.ElementAt(0).GetType() == typeof(ContainerElement) &&
                            element.Elements.ElementAt(1).GetType() == typeof(ClassifiedTextElement))
                        {
                            ClassifiedTextElement textElement = element.Elements.ElementAt(1) as ClassifiedTextElement;
                            if (textElement.Runs.Count() == 1)
                            {
                                ClassifiedTextRun run = textElement.Runs.ElementAt(0);
                                return run.Text;
                            }
                            else if (textElement.Runs.Count() > 1)
                            {
                                var str = new StringBuilder();
                                foreach (var run in textElement.Runs)
                                {
                                    str.Append(run.Text);
                                }
                                return str.ToString();
                            }
                        }
                        break;
                    }
                    break;
                }
            }
            return null;
        }

        private static string TryGetCppMethodInformation(IEnumerable<object> elements)
        {
            if (elements.Count() > 3 &&
                elements.ElementAt(2).GetType() == typeof(ContainerElement))
            {
                var element = elements.ElementAt(2) as ContainerElement;
                if (element.Elements.Count() == 1 &&
                    element.Elements.ElementAt(0).GetType() == typeof(ClassifiedTextElement))
                {
                    var textElement = element.Elements.ElementAt(0) as ClassifiedTextElement;
                    if (textElement.Runs.Count() == 1)
                    {
                        var run = textElement.Runs.ElementAt(0);
                        return run.Text;
                    }
                    else if (textElement.Runs.Count() > 1)
                    {
                        if (textElement.Runs.ElementAt(0).Text.IndexOf("扩展到:") != -1 ||
                            textElement.Runs.ElementAt(0).Text.IndexOf("大小:") != -1)
                        {
                            return null;
                        }

                        var str = new StringBuilder();
                        foreach (var run in textElement.Runs)
                        {
                            var temp = run.Text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "");
                            StringPretreatment(ref temp);
                            str.Append(temp);
                        }
                        return str.ToString();
                    }
                }
            }
            return null;
        }

        private static void CreateClassifiedTextRun(List<ApiResponse> formats, out List<ClassifiedTextRun> runs)
        {
            runs = new List<ClassifiedTextRun>();

            for (int i = 0; i < formats.Count; i++)
            {
                var temp = (i == formats.Count - 1) ? "" : "\n";
                if (formats[i].Success)
                {
                    runs.Add(new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Keyword, $"[ok]"));
                    runs.Add(new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Comment, $"{formats[i].TargetText}{temp}"));
                }
                else if (string.IsNullOrEmpty(formats[i].TargetText))
                {
                    runs.Add(new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.String, $"[{formats[i].Message}]"));
                    runs.Add(new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Comment, $"{formats[i].SourceText}{temp}"));
                }
                else
                {
                    runs.Add(new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Keyword, $"[{formats[i].Message}]"));
                    runs.Add(new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Comment, $"{formats[i].TargetText}{temp}"));
                }
            }
        }

        private static void StringPretreatment(ref string str)
        {
            var count = 0;
            var temp = str.TrimStart();
            foreach (var s in temp)
            {
                if (char.IsPunctuation(s))
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            str = temp.Substring(count).Trim();
        }

    }
}
