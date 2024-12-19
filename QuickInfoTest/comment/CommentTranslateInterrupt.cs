using System.Text.RegularExpressions;

namespace CommentTranslator22.Popups.QuickInfo.Comment
{
    internal static class CommentTranslateInterrupt
    {
        ///// <summary>
        ///// 检查这段注释文本是不是标签信息或者代码
        ///// </summary>
        ///// <param name="text"></param>
        ///// <returns>如果为true表示不属于标签信息或代码，否则为false</returns>
        //public static bool Check(string text)
        //{
        //    if (CommentTranslator22Package.Config.UseMask)
        //    {
        //        foreach (var str in CommentTranslator22Package.Config.UseMaskType)
        //        {
        //            if (Regex.IsMatch(text, WildCardToRegular(str)))
        //                return true;
        //        }
        //    }
        //    return false;
        //}

        //public static bool Check(string text, string str)
        //{
        //    if (Regex.IsMatch(text, WildCardToRegular(str)))
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        ///// If you want to implement both "*" and "?"
        //private static string WildCardToRegular(string value)
        //{
        //    return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        //}
    }
}
