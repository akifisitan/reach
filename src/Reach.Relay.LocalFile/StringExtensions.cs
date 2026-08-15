using System.Text;

namespace Reach.Relay.LocalFile;

internal static class StringExtensions
{
    extension(string content)
    {
        public string DecodeBase64() => Encoding.UTF8.GetString(Convert.FromBase64String(content));

        public string EncodeBase64() => Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
    }
}
