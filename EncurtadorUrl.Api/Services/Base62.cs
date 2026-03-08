using System.Security.Cryptography;
using System.Text;

namespace UrlShortener.Api.Services;

public static class Base62
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private static readonly int Radix = Alphabet.Length;

    public static string Encode(ulong value)
    {
        if (value == 0) return Alphabet[0].ToString();

        var sb = new StringBuilder();
        while (value > 0)
        {
            var rem = (int)(value % (ulong)Radix);
            sb.Insert(0, Alphabet[rem]);
            value /= (ulong)Radix;
        }

        return sb.ToString();
    }

    public static string GenerateRandom(int length)
    {
        if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));

        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            int idx = RandomNumberGenerator.GetInt32(Radix);
            chars[i] = Alphabet[idx];
        }
        return new string(chars);
    }
}