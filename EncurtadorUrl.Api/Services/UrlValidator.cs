using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Services;

public static class UrlValidator
{
    public static void EnsureValidHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ValidationException("originalUrl é obrigatório.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ValidationException("originalUrl deve ser uma URL válida.");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ValidationException("originalUrl deve começar com http:// ou https://.");
    }

    public static void EnsureValidAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new ValidationException("customAlias não pode ser vazio.");

        if (alias.Length < 3 || alias.Length > 12)
            throw new ValidationException("customAlias deve ter entre 3 e 12 caracteres.");

        // Permitimos apenas: A-Z a-z 0-9 - _
        foreach (var ch in alias)
        {
            var ok = char.IsLetterOrDigit(ch) || ch == '-' || ch == '_';
            if (!ok)
                throw new ValidationException("customAlias permite apenas letras, números, '-' e '_'.");
        }
    }
}