using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Expat;

using static NativeMethods;

public sealed partial class XmlParser : IDisposable
{
    readonly XmlParserOptions _options;
    volatile bool _disposed, _inCdata;

    ArrayBufferWriter<byte>? _textContent;
    ArrayBufferWriter<byte>? _cdataSection;

    nint _handle;
    GCHandle _userData;
    Exception? _lastException;

    public XmlParserOptions Options => _options;

    static readonly Dictionary<XmlEncoding, string?> s_XmlEncodingNameMap = new()
    {
        [XmlEncoding.Default] = default,
        [XmlEncoding.Ascii] = "US-ASCII",
        [XmlEncoding.Utf8] = "UTF-8",
        [XmlEncoding.Utf16Le] = "UTF-16LE",
        [XmlEncoding.Utf16Be] = "UTF-16BE",
        [XmlEncoding.Iso88591] = "ISO-8859-1"
    };

    void OnUnhandledException(Exception ex)
    {
        _lastException ??= ex;
        XML_StopParser(_handle, false);
    }

    public XmlParser(XmlParserOptions? options = default)
    {
        _options = options ?? XmlParserOptions.Default;

        var encodingName = s_XmlEncodingNameMap[_options.Encoding];

        if (_options.NamespaceSeparatorChar is byte nsByte)
            _handle = XML_ParserCreateNS(encodingName, nsByte);
        else
            _handle = XML_ParserCreate(encodingName);

        if (_handle == 0)
            throw new InvalidOperationException("Cannot create expat parser.");

        _userData = GCHandle.Alloc(this);

        InitNative();
    }

    void FlushText()
    {
        if (_textContent is not { WrittenCount: > 0 }) return;

        try
        {
            var span = _textContent.WrittenSpan;

            if (_options.IgnoreWhitespace
                && span.IndexOfAnyExcept(s_WhitespaceChars) < 0) return;

            OnText?.Invoke(span);
        }
        catch (Exception ex)
        {
            OnUnhandledException(ex);
        }
        finally
        {
            _textContent.Clear();
        }
    }

    static readonly SearchValues<byte> s_WhitespaceChars
        = SearchValues.Create((byte)' ', (byte)'\n', (byte)'\r', (byte)'\t');

    [MethodImpl(MethodImplOptions.NoInlining)]
    unsafe void InitNative()
    {
        XML_SetUserData(_handle, (nint)_userData);
        XML_SetParamEntityParsing(_handle, 0);
        XML_SetProcessingInstructionHandler(_handle, &ProcessingInstructionCallback);
        XML_SetCharacterDataHandler(_handle, &CharacterDataCallback);
        XML_SetStartDoctypeDeclHandler(_handle, &StartDoctypeCallback);
        XML_SetCommentHandler(_handle, &CommentCallback);
        XML_SetElementHandler(_handle, &StartElementCallback, &EndElementCallback);
        XML_SetCdataSectionHandler(_handle, &CdataSectionStartCallback, &CdataSectionEndCallback);
        XML_SetNamespaceDeclHandler(_handle, &StartNamespaceCallback, &EndNamespaceCallback);

        if (_options.HashSaltFactory is { } factory)
        {
            fixed (byte* p = stackalloc byte[16])
            {
                var span = new Span<byte>(p, 16);

                factory(span);

                XML_SetHashSalt16Bytes(_handle, p);
            }
        }
    }

    void ThrowLastException()
    {
        if (_lastException is { } ex)
            ExceptionDispatchInfo.Throw(ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public unsafe void Parse(ReadOnlySpan<byte> bytes, bool is_final)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ThrowLastException();

        XmlStatus result;

        fixed (byte* ptr = bytes)
            result = XML_Parse(_handle, ptr, bytes.Length, is_final);

        ThrowLastException();

        if (result != XmlStatus.Success)
            ThrowIfFailed();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!XML_ParserReset(_handle, s_XmlEncodingNameMap[_options.Encoding]))
        {
            ThrowIfFailed();
            return;
        }

        _textContent?.Clear();
        _textContent = null;

        _inCdata = false;

        _cdataSection?.Clear();
        _cdataSection = null;

        _lastException = null;

        InitNative();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Suspend(bool resumable)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (XML_StopParser(_handle, resumable) != XmlStatus.Success)
            ThrowIfFailed();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Resume()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (XML_ResumeParser(_handle) != XmlStatus.Success)
            ThrowIfFailed();
    }

    bool TryCreateExpatException([NotNullWhen(true)] out ExpatException? exception)
    {
        exception = default;

        var code = XML_GetErrorCode(_handle);

        if (code == 0) return false;

        var lineNum = XML_GetCurrentLineNumber(_handle);

        var linePos = XML_GetCurrentColumnNumber(_handle);

        exception = ExpatException.Create(code, lineNum, linePos);

        return exception != null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void ThrowIfFailed()
    {
        if (TryCreateExpatException(out var ex))
            throw ex;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            XML_StopParser(_handle, false);

            _lastException = null;

            _textContent?.Clear();
            _textContent = null;

            _cdataSection?.Clear();
            _cdataSection = null;

            XML_ParserFree(_handle);

            if (_userData.IsAllocated)
                _userData.Free();

            _handle = 0;
        }
    }
}
