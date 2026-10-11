using System.Runtime.InteropServices;

namespace Expat.Test;

struct NativeXmlParser : IDisposable
{
    readonly string? _encoding;
    GCHandle _userData;
    nint _parser;

    public NativeXmlParser(string? encoding, object? userData = default)
    {
        _encoding = encoding;

        _parser = XML_ParserCreate(encoding);

        _userData = userData is null ? default : GCHandle.Alloc(userData);

        XML_SetUserData(_parser, (nint)_userData);
    }

    public readonly void Reset()
    {
        if (!XML_ParserReset(_parser, _encoding))
        {
            var code = XML_GetErrorCode(_parser);

            if (code != XmlError.None)
                throw ExpatException.Create(code);
        }

        XML_SetUserData(_parser, (nint)_userData);
    }

    public void Dispose()
    {
        if (_parser != 0)
        {
            XML_ParserFree(_parser);
            _parser = 0;
        }

        if (_userData.IsAllocated)
        {
            _userData.Free();
            _userData = default;
        }
    }

    public static implicit operator nint(NativeXmlParser parser) => parser._parser;
}
