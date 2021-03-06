using System.Collections.Generic;

namespace CommentTranslator.Parsers
{
    public class XmlCommentParser : CommentParser
    {
        public XmlCommentParser()
        {
            Tags = new List<ParseTag>
            {
                //Multi line comment
                new ParseTag(){
                    Start = "<!--",
                    End = "-->",
                    Name = "multiline"
                }
            };
        }
    }
}
