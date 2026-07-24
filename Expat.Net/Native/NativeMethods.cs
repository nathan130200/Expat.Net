using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Expat.Native;

#pragma warning disable

[EditorBrowsable(EditorBrowsableState.Never)]
public static class NativeMethods
{
	const string LibraryName = "expat";

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetXmlDeclHandler(nint parser, XmlPrologHandler handler);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern nint XML_ParserCreate(string? encoding);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool XML_ParserReset(nint parser, string? encoding);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetElementHandler(nint parser, XmlStartElementHandler start, XmlEndElementHandler end);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCharacterDataHandler(nint parser, XmlCharacterDataHandler handler);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetProcessingInstructionHandler(nint parser, XmlProcessingInstructionHandler handler);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCommentHandler(nint parser, XmlCommentHandler handler);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCdataSectionHandler(nint parser, XmlStartCdataSectionHandler start, XmlEndCdataSectionHandler end);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetUserData(nint parser, nint userData);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlError XML_UseForeignDTD(nint parser, [MarshalAs(UnmanagedType.I1)] bool useDTD);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetSpecifiedAttributeCount(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlStatus XML_Parse(nint parser, nint buf, int len, [MarshalAs(UnmanagedType.Bool)] bool isFinal);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlStatus XML_StopParser(nint parser, [MarshalAs(UnmanagedType.I1)] bool resumable);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlStatus XML_ResumeParser(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlError XML_SetParamEntityParsing(nint parser, XmlParamEntityParsing state);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static unsafe extern bool XML_SetHashSalt16Bytes(nint parser, byte* entropy);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlError XML_GetErrorCode(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentLineNumber(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentColumnNumbers(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentByteIndex(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentByteCount(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_ParserFree(nint parser);

	[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
	public static extern nint XML_ErrorString(XmlError error);

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