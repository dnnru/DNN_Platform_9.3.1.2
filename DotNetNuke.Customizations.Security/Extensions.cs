#region Usings

using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using DotNetNuke.Customizations.Security.HtmlSanitizer;

#endregion

namespace DotNetNuke.Customizations.Security
{
    public static class Extensions
    {
        public static string StripHtml(this string input)
        {
            var dom = HtmlSanitizer.HtmlSanitizer.CreateParser().Parse($"<html><body>{input}</body></html>");
            foreach (IHtmlScriptElement htmlScriptElement in dom.QuerySelectorAll<IHtmlScriptElement>("script"))
            {
                htmlScriptElement.Remove();
            }

            foreach (IHtmlStyleElement htmlStyleElement in dom.QuerySelectorAll<IHtmlStyleElement>("style"))
            {
                htmlStyleElement.Remove();
            }

            return dom.DocumentElement.ToHtml(new PlainTextMarkupFormatter());
        }
    }
}
