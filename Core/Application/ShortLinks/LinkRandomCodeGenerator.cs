using System.Security.Cryptography;
using Application.Abstractions;

namespace Application.ShortLinks;

internal sealed class LinkRandomCodeGenerator : ILinkCodeGenerator
{
    public const int CodeLength = 8;
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public string Generate()
    {
        Span<char> chars = stackalloc char[CodeLength];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return new string(chars);
    }
}
