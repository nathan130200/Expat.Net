using System.Runtime.InteropServices;

namespace Expat;

partial class PInvoke
{
	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetElementHandler(nint parser, XML_StartElementHandler start, XML_EndElementHandler end);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCharacterDataHandler(nint parser, XML_CharacterDataHandler handler);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetProcessingInstructionHandler(nint parser, XML_ProcessingInstructionHandler handler);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCommentHandler(nint parser, XML_CommentHandler handler);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetCdataSectionHandler(nint parser, XML_CdataSectionHandler start, XML_CdataSectionHandler end);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetNamespaceDeclHandler(nint parser, XML_StartNamespaceDeclHandler start, XML_EndElementHandler end);
}