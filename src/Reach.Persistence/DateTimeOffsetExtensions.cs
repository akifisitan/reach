using System.Globalization;

namespace Reach.Persistence;

public static class DateTimeOffsetExtensions
{
    extension(DateTimeOffset date)
    {
        public string ToIsoString() => date.ToString("O");

        public static DateTimeOffset FromIsoString(string text) =>
            DateTimeOffset.ParseExact(
                text,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            );

        public long ToDbTimestamp() => date.ToUnixTimeMilliseconds();

        public static DateTimeOffset FromDbTimestamp(long milliseconds) =>
            DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }
}
