using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Expat.Native;

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void PrologHandlerCallback(nint userData, nint versionPtr, nint encodingPtr, XmlStandalone standalone);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void StartElementHandlerCallback(nint userData, nint namePtr, nint attsPtr);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void EndElementHandlerCallback(nint userData, nint namePtr);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void CharacterDataHandlerCallback(nint userData, nint buf, int len);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ProcessingInstructionHandlerCallback(nint userData, nint targetPtr, nint dataPtr);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void CommentHandlerCallback(nint userData, nint dataPtr);

[EditorBrowsable(EditorBrowsableState.Never)]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void CdataSectionHandlerCallback(nint userData);