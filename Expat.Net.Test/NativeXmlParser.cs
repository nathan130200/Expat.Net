namespace Expat.Test;

readonly struct NativeXmlParser(string encoding = "UTF-8") : IDisposable
{
    readonly nint _handle = XML_ParserCreate(encoding);

    public void Dispose()
    {
        XML_ParserFree(_handle);
    }

    public static implicit operator nint(NativeXmlParser parser) => parser._handle;
}
