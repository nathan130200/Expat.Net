using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Expat.Native;

#pragma warning disable

[EditorBrowsable(EditorBrowsableState.Never)]
public unsafe static class NativeMethods
{
	const string LibraryName = "expat";

	static readonly nint s_LibraryHandle;

	static nint TryLoadLibrary()
	{
		IEnumerable<string> names = ["libexpat", "expat"];

		IEnumerable<string> extensions;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			extensions = [".dll"];
		else
		{
			extensions = [".so", ".so.1"];

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				extensions = [".dylib", .. extensions];
		}

		var candidates = from name in names
						 from extension in extensions
						 select string.Concat(name, extension);

		nint ptr;

		foreach (var candidate in candidates)
		{
			if (NativeLibrary.TryLoad(candidate, out ptr))
			{
				Trace.WriteLine($"<LibExpat> Resolved from file: '{candidate}' (0x{ptr:x8})");
				return ptr;
			}
		}

		var fromEnv = Environment.GetEnvironmentVariable("EXPAT_LIBRARY_PATH");

		if (File.Exists(fromEnv) && NativeLibrary.TryLoad(fromEnv, out ptr))
		{
			Trace.WriteLine($"<LibExpat> Resolved from environment: '{fromEnv}' (0x{ptr:x8})");
			return ptr;
		}

		Trace.WriteLine("<LibExpat> Unable to resolve expat! Fallback to .NET library loader...");

		return 0;
	}

	static Dictionary<XmlError, string> s_ErrorMessageCache = new();

	static NativeMethods()
	{
		s_LibraryHandle = TryLoadLibrary();

		NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, (name, _, _) =>
		{
			if (name == LibraryName)
				return s_LibraryHandle;

			return (nint)0;
		});

		var version = XML_ExpatVersionInfo();

		Trace.WriteLine($"<LibExpat> Using expat version: {version.Major}.{version.Minor}.{version.Build}");

		foreach (var value in Enum.GetValues<XmlError>())
		{
			var ptr = XML_ErrorString(value);

			if (Marshal.PtrToStringUTF8(ptr) is string msg)
				s_ErrorMessageCache[value] = msg;
		}

		s_ErrorMessageCache[0] = "none";
	}

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetXmlDeclHandler(nint parser, PrologHandlerCallback handler);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern nint XML_ParserCreate(string? encoding);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool XML_ParserReset(nint parser, string? encoding);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetElementHandler(nint parser, StartElementHandlerCallback start, EndElementHandlerCallback end);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCharacterDataHandler(nint parser, CharacterDataHandlerCallback handler);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetProcessingInstructionHandler(nint parser, ProcessingInstructionHandlerCallback handler);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCommentHandler(nint parser, CommentHandlerCallback handler);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCdataSectionHandler(nint parser, CdataSectionHandlerCallback start, CdataSectionHandlerCallback end);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetUserData(nint parser, nint userData);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlError XML_UseForeignDTD(nint parser, [MarshalAs(UnmanagedType.I1)] bool useDTD);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetSpecifiedAttributeCount(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlStatus XML_Parse(nint parser, byte* buf, int len, [MarshalAs(UnmanagedType.Bool)] bool isFinal);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlStatus XML_StopParser(nint parser, [MarshalAs(UnmanagedType.I1)] bool resumable);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlStatus XML_ResumeParser(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlError XML_SetParamEntityParsing(nint parser, XmlParamEntityParsing state);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool XML_SetHashSalt16Bytes(nint parser, byte* entropy);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlError XML_GetErrorCode(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentLineNumber(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentColumnNumber(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentByteIndex(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentByteCount(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern nint XML_GetInputContext(nint parser, out int offset, out int size);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_ParserFree(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	static extern nint XML_ErrorString(XmlError error);

	public static string XML_GetErrorMessage(XmlError error) => s_ErrorMessageCache[error];

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern ExpatVersion XML_ExpatVersionInfo();

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool XML_SetBillionLaughsAttackProtectionMaximumAmplification(nint parser, float maximumAmplificationFactor);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool XML_SetBillionLaughsAttackProtectionActivationThreshold(nint parser, long activationThresholdBytes);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool XML_SetAllocTrackerActivationThreshold(nint parser, long activationThresholdBytes);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool XML_SetAllocTrackerMaximumAmplification(nint parser, float maximumAmplificationFactor);
}