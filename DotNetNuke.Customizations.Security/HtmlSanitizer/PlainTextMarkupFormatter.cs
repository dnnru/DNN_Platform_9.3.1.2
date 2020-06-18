#region Usings

using System;
using AngleSharp;
using AngleSharp.Dom;

#endregion

namespace DotNetNuke.Customizations.Security.HtmlSanitizer
{
    public class PlainTextMarkupFormatter : IMarkupFormatter
    {
        public string Text(string text)
        {
            return text;
        }

        public string Comment(IComment comment)
        {
            return string.Empty;
        }

        public string Processing(IProcessingInstruction processing)
        {
            return string.Empty;
        }

        public string Doctype(IDocumentType doctype)
        {
            return string.Empty;
        }

        public string OpenTag(IElement element, bool selfClosing)
        {
            switch (element.LocalName)
            {
                case "p":
                    return Environment.NewLine;
                case "br":
                    return Environment.NewLine;
            }

            return string.Empty;
        }

        public string CloseTag(IElement element, bool selfClosing)
        {
            return string.Empty;
        }

        public string Attribute(IAttr attribute)
        {
            return string.Empty;
        }
    }
}
