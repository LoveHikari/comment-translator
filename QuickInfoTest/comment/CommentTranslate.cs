using CommentTranslator22.Popups.QuickInfo.Comment.Support;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommentTranslator22.Popups.QuickInfo.Comment
{
    internal static class CommentTranslate
    {
        /// <summary>
        /// 尝试翻译方法或变量的注释文本
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ClassifiedTextRun>> TryTranslateMethodInformationAsync(IAsyncQuickInfoSession session, string typeName)
        {
            var str = TryGetMethodInformation(session, typeName);
            if (str != null)
            {
                CommentHelp.StringPretreatment(ref str);
                //var s = TranslateClient.Instance.HumpUnfold(str);
                var s = str;
                //var r = MethodAnnotationData.Instance.IndexOf(s) ?? GeneralAnnotationData.Instance.IndexOf(s);
                //if (r == null)
                //{
                    var recv = await TranslateClient.Instance.TranslateAsync(s);
                //    if (string.IsNullOrEmpty(recv.TargetText) == false)
                //    {
                //        // 在这里将翻译后的方法注释保存到 ?? 方法中
                //        MethodAnnotationData.Instance.Add(recv);
                //    }

                    var temp = new List<ApiResponse> { recv };
                    CreateClassifiedTextRun(temp, out var runs);
                    return runs;
                //}
                //else
                //{
                //    var recv = new ApiRecvFormat()
                //    {
                //        Message = "buf",
                //        TargetText = r,
                //    };

                //    var temp = new List<ApiRecvFormat> { recv };
                //    CreateClassifiedTextRun(temp, out var runs);
                //    return runs;
                //}
            }
            return null;
        }

        /// <summary>
        /// 尝试从 IAsyncQuickInfoSession 中获取鼠标指向的方法或变量的注解
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static string TryGetMethodInformation(IAsyncQuickInfoSession session, string typeName)
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

        static string TryGetCppMethodInformation(IEnumerable<object> elements)
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
                            CommentHelp.StringPretreatment(ref temp);
                            str.Append(temp);
                        }
                        return str.ToString();
                    }
                }
            }
            return null;
        }

        private static void CreateClassifiedTextRun(List<ApiRecvFormat> formats, out List<ClassifiedTextRun> runs)
        {
            runs = new List<ClassifiedTextRun>();

            for (int i = 0; i < formats.Count; i++)
            {
                var temp = (i == formats.Count - 1) ? "" : "\n";
                if (formats[i].IsSuccess)
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


        /// <summary>
        /// 拆分字符串，获取词组集合
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        static IEnumerable<object> GetWords(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 2 || text.Length > 50)
            {
                return null;
            }

            // 先按下划线分割这个字符串
            var strList = new List<object>();
            var strings = text.Split('_');
            foreach (var s in strings)
            {
                // 将所有非字母字符替换为空
                var temp = Regex.Replace(s, "[^A-Za-z]", "");
                if (temp.Length < 2)
                {
                    continue;
                }

                // 检查是否全部为大写字母
                if (Regex.IsMatch(temp, "^[A-Z]+$"))
                {
                    strList.Add(temp);
                    continue;
                }

                // 匹配以大写字母开始后跟随一个或多个小写字母的单词,
                // 如果字符串以小写字母开始，则这些小写字母序列也算匹配（因为^可以匹配到字符串的开始，意味着紧跟其后的[a-z]+可以开始匹配）
                var matches = Regex.Matches(temp, "([A-Z]|^)[a-z]+");
                foreach (var macth in matches)
                {
                    strList.Add(macth);
                }
            }
            return strList;
        }

    }
}
