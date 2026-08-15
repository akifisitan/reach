using System.Net;
using System.Text;
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

        public string DecodeBase64() => Encoding.UTF8.GetString(Convert.FromBase64String(content));

        public string EncodeBase64() => Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
    }
}
