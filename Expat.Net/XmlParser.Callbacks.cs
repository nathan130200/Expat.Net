using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Expat;

public delegate void StartElementEventHandler(ReadOnlySpan<byte> name, XmlAttributesReader attributes);

public delegate void EndElementEventHandler(ReadOnlySpan<byte> name);

public delegate void StartNamespaceDeclarationEventHandler(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> uri);

public delegate void EndNamespaceDeclarationEventHandler(ReadOnlySpan<byte> prefix);

public delegate void CharacterDataEventHandler(ReadOnlySpan<byte> content);

public delegate void CommentEventHandler(ReadOnlySpan<byte> comment);

public delegate void ProcessingInstructionEventHandler(ReadOnlySpan<byte> target, ReadOnlySpan<byte> data);

partial class XmlParser
{
    public event ProcessingInstructionEventHandler? OnProcessingInstruction;

    public event StartElementEventHandler? OnStartElement;

    public event EndElementEventHandler? OnEndElement;

    public event StartNamespaceDeclarationEventHandler? OnStartNamespaceDeclaration;

    public event EndNamespaceDeclarationEventHandler? OnEndNamespaceDeclaration;

    public event CharacterDataEventHandler? OnText;

    public event CharacterDataEventHandler? OnCdata;

    public event CommentEventHandler? OnComment;

    static bool GetParserContext(nint userData, [NotNullWhen(true)] out XmlParser? parser)
    {
        parser = default;

        var handle = GCHandle.FromIntPtr(userData);

        if (handle.Target is XmlParser p)
            parser = p;

        return parser != null;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void ProcessingInstructionCallback(nint userData, nint target, nint data)
    {
        if (!GetParserContext(userData, out var parser)) return;

        parser.FlushText();

        if (parser.OnProcessingInstruction is not { } handler) return;

        try
        {
            var targetSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)target);

            var dataSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)data);

            handler(targetSpan, dataSpan);
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void StartDoctypeCallback(nint userData, nint doctype, nint systemId, nint publicId, int hasExternalSubset)
    {
        if (!GetParserContext(userData, out var parser)) return;

        parser.OnUnhandledException(new NotSupportedException("DTD is not supported."));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void StartElementCallback(nint userData, nint namePtr, nint attsListPtr)
    {
        if (!GetParserContext(userData, out var parser)) return;

        try
        {
            parser.FlushText();

            var handler = parser.OnStartElement;

            if (handler == null) return;

            var numAttributes = NativeMethods.XML_GetSpecifiedAttributeCount(parser._handle);

            var enumerator = new XmlAttributesReader(attsListPtr, numAttributes);

            handler(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)namePtr), enumerator);
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void EndElementCallback(nint userData, nint namePtr)
    {
        if (!GetParserContext(userData, out var parser)) return;

        try
        {
            parser.FlushText();

            var handler = parser.OnEndElement;

            if (handler == null) return;

            handler(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)namePtr));
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void StartNamespaceCallback(nint userData, nint prefix, nint uri)
    {
        if (!GetParserContext(userData, out var parser)) return;

        try
        {
            var handler = parser.OnStartNamespaceDeclaration;

            if (handler == null) return;

            var prefixSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)prefix);

            var uriSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)uri);

            handler(prefixSpan, uriSpan);
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void EndNamespaceCallback(nint userData, nint prefix)
    {
        if (!GetParserContext(userData, out var parser)) return;

        try
        {
            var handler = parser.OnEndNamespaceDeclaration;

            if (handler == null) return;

            handler(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)prefix));
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void CdataSectionStartCallback(nint userData)
    {
        if (!GetParserContext(userData, out var parser)) return;

        try
        {
            parser.FlushText();

            parser._inCdata = true;

            if (parser.OnCdata == null) return;

            parser._cdataSection ??= new();
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void CdataSectionEndCallback(nint userData)
    {
        if (!GetParserContext(userData, out var parser)) return;

        try
        {
            var handler = parser.OnCdata;

            var buf = parser._cdataSection;

            if (handler != null && buf != null)
                handler(parser._cdataSection.WrittenSpan);
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
        finally
        {
            parser._inCdata = false;

            parser._cdataSection?.Clear();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void CommentCallback(nint userData, nint s)
    {
        if (!GetParserContext(userData, out var parser)) return;

        parser.FlushText();

        if (parser.OnComment is not { } handler) return;

        try
        {
            var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)s);

            handler(span);
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void CharacterDataCallback(nint userData, byte* s, int count)
    {
        if (!GetParserContext(userData, out var parser)) return;

        try
        {
            if (parser._inCdata)
            {
                if (parser._cdataSection is null) return;

                var span = parser._cdataSection.GetSpan(count);

                new ReadOnlySpan<byte>(s, count).CopyTo(span);

                parser._cdataSection.Advance(count);
            }
            else
            {
                if (parser.OnText is null) return;

                parser._textContent ??= new();

                var span = parser._textContent.GetSpan(count);

                new ReadOnlySpan<byte>(s, count).CopyTo(span);

                parser._textContent.Advance(count);
            }
        }
        catch (Exception ex)
        {
            parser.OnUnhandledException(ex);
        }
    }
}
