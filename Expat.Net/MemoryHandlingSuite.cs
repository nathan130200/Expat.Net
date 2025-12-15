using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Expat;

public abstract class MemoryHandlingSuite
{
	public static readonly MemoryHandlingSuite Default = new DefaultMemoryHandlingSuite();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public delegate nint MallocDeleg(int size);

	[EditorBrowsable(EditorBrowsableState.Never)]
	public delegate nint ReallocDeleg(nint ptr, int size);

	[EditorBrowsable(EditorBrowsableState.Never)]
	public delegate void FreeDeleg(nint ptr);

	[StructLayout(LayoutKind.Sequential)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public struct Struct
	{
		public MallocDeleg m_MallocFunction;
		public ReallocDeleg m_ReallocFunction;
		public FreeDeleg m_FreeFunction;
	}

	class DefaultMemoryHandlingSuite : MemoryHandlingSuite
	{

	}

	internal Struct __native;

	public MemoryHandlingSuite()
	{
		__native = new Struct
		{
			m_MallocFunction = Alloc,
			m_ReallocFunction = Realloc,
			m_FreeFunction = Free
		};
	}

	public virtual nint Alloc(int size) => Marshal.AllocHGlobal(size);

	public virtual nint Realloc(nint ptr, int size) => Marshal.ReAllocHGlobal(ptr, size);

	public virtual void Free(nint ptr) => Marshal.FreeHGlobal(ptr);
}
