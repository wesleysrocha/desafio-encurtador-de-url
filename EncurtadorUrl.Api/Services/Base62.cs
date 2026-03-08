using System.Text;

namespace UrlShortener.Api.Services;

public static class Base62
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string Encode(long value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (value == 0) return Alphabet[0].ToString();

        var sb = new StringBuilder();

        while (value > 0)
        {
            var remainder = (int)(value % 62);
            sb.Insert(0, Alphabet[remainder]);
            value /= 62;
        }

        return sb.ToString();
    }
}