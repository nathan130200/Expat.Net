using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Expat;

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_StartElementHandler(nint userData, nint name, nint atts);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_EndElementHandler(nint userData, nint name);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_CharacterDataHandler(nint userData, nint buf, int len);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_ProcessingInstructionHandler(nint userData, nint target, nint data);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_CommentHandler(nint userData, nint data);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_CdataSectionHandler(nint userData);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_StartNamespaceDeclHandler(nint userData, nint prefix, nint uri);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_EndNamespaceDeclHandler(nint userData, nint prefix);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XML_XmlDeclHandler(nint userData, nint version, nint encoding, int standalone);