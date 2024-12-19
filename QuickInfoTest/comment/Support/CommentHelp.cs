
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace CommentTranslator22.Popups.QuickInfo.Comment.Support
{
    internal class CommentHelp
    {



        public static void StringPretreatment(ref string str)
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
