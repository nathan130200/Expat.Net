using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Expat.Native;

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XmlPrologHandler(nint userData, nint versionPtr, nint encodingPtr, XmlStandalone standalone);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XmlStartElementHandler(nint userData, nint namePtr, nint attsPtr);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XmlEndElementHandler(nint userData, nint namePtr);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XmlCharacterDataHandler(nint userData, nint buf, int len);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XmlProcessingInstructionHandler(nint userData, nint targetPtr, nint dataPtr);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XmlCommentHandler(nint userData, nint dataPtr);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XmlStartCdataSectionHandler(nint userData);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void XmlEndCdataSectionHandler(nint userData);