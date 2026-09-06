using System.Net;
using HtmlAgilityPack;

namespace Reach.Relay.Confluence;

internal static class StringExtensions
{
    extension(string content)
    {
        public string GetHtmlInnerText()
        {
            var html = new HtmlDocument();

            html.LoadHtml(content);

            return WebUtility.HtmlDecode(html.DocumentNode.InnerText);
        }
    }
}
