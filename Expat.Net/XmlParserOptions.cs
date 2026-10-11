using System.Security.Cryptography;

namespace Expat;

public sealed class XmlParserOptions
{
    public static readonly XmlParserOptions Default = new();

    public XmlParserOptions()
    {

    }

    public XmlParserOptions(XmlParserOptions other)
    {
        Encoding = other.Encoding;
        NamespaceSeparatorChar = other.NamespaceSeparatorChar;
    }

    public XmlEncoding Encoding
    {
        get;

        init
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentException("Invalid XML encoding.");

            field = value;
        }

    } = XmlEncoding.Utf8;

    public byte? NamespaceSeparatorChar { get; init; }

    public bool IgnoreWhitespace { get; init; } = true;

    public HashSaltFactoryFunction? HashSaltFactory { get; init; }

    public static void DefaultHashSaltFactory(Span<byte> bytes)
    {
        RandomNumberGenerator.Fill(bytes);
    }

    public delegate void HashSaltFactoryFunction(Span<byte> bytes);
}
