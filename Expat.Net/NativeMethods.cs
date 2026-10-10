using System.Reflection;
using System.Runtime.InteropServices;

namespace Expat;

using unsafe XML_CharacterDataHandler = delegate* unmanaged[Cdecl]<nint, byte*, int, void>;

#pragma warning disable

internal static unsafe partial class NativeMethods
{
    const string LibraryName = "expat";

    static readonly nint s_LibraryInstance;

    static readonly IReadOnlyDictionary<XmlError, string> s_XmlErrorToStringMap;

    static IEnumerable<string> GetCandidates()
    {
        string[] names = ["expat", "libexpat"];

        string[] extensions;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            extensions = [".dll"];
        else
        {
            extensions = [".so", ".so.1"];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                extensions = [".dylib", .. extensions];
        }

        return from name in names
               from extension in extensions
               select string.Concat(name, extension);
    }

    static void TryLoadLibrary(out nint handle)
    {
        handle = default;

        foreach (var candidate in GetCandidates())
        {
            if (NativeLibrary.TryLoad(candidate, out handle))
                break;
        }
    }

    static NativeMethods()
    {
        TryLoadLibrary(out s_LibraryInstance);

        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ResolveExpatLibrary);

        var enumValues = Enum.GetValues<XmlError>();

        var dict = new Dictionary<XmlError, string>();

        foreach (var iter in enumValues)
        {
            var ptr = XML_ErrorString_Impl(iter);

            if (Marshal.PtrToStringUTF8(ptr) is not string str) continue;

            dict[iter] = str;
        }

        s_XmlErrorToStringMap = dict;
    }

    static nint ResolveExpatLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == LibraryName)
            return s_LibraryInstance;

        return 0;
    }

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint XML_ParserCreateNS(string? encoding, byte namespaceSeparator);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint XML_ParserCreate(string? encoding);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_ParserFree(nint parser);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool XML_ParserReset(nint parser, string? encoding);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetStartDoctypeDeclHandler(nint parser, delegate* unmanaged[Cdecl]<nint, nint, nint, nint, int, void> handler);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetProcessingInstructionHandler(nint parser, delegate* unmanaged[Cdecl]<nint, nint, nint, void> handler);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetHashSalt16Bytes(nint parser, byte* entropy);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetCommentHandler(nint parser, delegate* unmanaged[Cdecl]<nint, nint, void> handler);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetElementHandler(nint parser, delegate* unmanaged[Cdecl]<nint, nint, nint, void> start, delegate* unmanaged[Cdecl]<nint, nint, void> end);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetCharacterDataHandler(nint parser, XML_CharacterDataHandler handler);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetCdataSectionHandler(nint parser, delegate* unmanaged[Cdecl]<nint, void> start, delegate* unmanaged[Cdecl]<nint, void> end);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetNamespaceDeclHandler(nint parser, delegate* unmanaged[Cdecl]<nint, nint, nint, void> start, delegate* unmanaged[Cdecl]<nint, nint, void> end);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void XML_SetUserData(nint parser, nint userData);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int XML_UseForeignDTD(nint parser, [MarshalAs(UnmanagedType.I1)] bool useDTD);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int XML_GetSpecifiedAttributeCount(nint parser);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static unsafe partial XmlStatus XML_Parse(nint parser, byte* s, int len, [MarshalAs(UnmanagedType.I4)] bool is_final);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial XmlStatus XML_StopParser(nint parser, [MarshalAs(UnmanagedType.I1)] bool resumable);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial XmlStatus XML_ResumeParser(nint parser);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool XML_SetParamEntityParsing(nint parser, int mode);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial XmlError XML_GetErrorCode(nint parser);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "XML_ErrorString")]
    private static partial nint XML_ErrorString_Impl(XmlError code);

    public static string? XML_ErrorString(XmlError code) => s_XmlErrorToStringMap.GetValueOrDefault(code);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int XML_GetCurrentLineNumber(nint parser);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int XML_GetCurrentColumnNumber(nint parser);
}