#pragma warning disable

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Expat.Net.Test")]

namespace Expat;

[EditorBrowsable(EditorBrowsableState.Never)]
internal static partial class Native
{
	const string s_LibName = "expat";

	static nint s_LibraryInstance;

	static readonly Lock s_Lock = new();

	static readonly Lazy<IEnumerable<string>> s_LibraryFileNames = new(() =>
	{
		List<string> result = [];

		string[] extensions;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			extensions = [".dll"];
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			extensions = [".dylib"];
		else
			extensions = [".so", ".so.1"];

		foreach (var extension in extensions)
		{
#if DEBUG
			result.Add($"expatd{extension}");
			result.Add($"libexpatd{extension}");
#endif

			result.Add($"expat{extension}");
			result.Add($"libexpat{extension}");
		}

		return result;

	}, true);

	static Native()
	{
		NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, static (libraryName, assembly, searchPaths) =>
		{
			if (libraryName == s_LibName)
			{
				lock (s_Lock)
				{
					if (s_LibraryInstance == 0)
					{
						foreach (var fileName in s_LibraryFileNames.Value)
						{
							if (NativeLibrary.TryLoad(fileName, assembly, searchPaths, out var result))
							{
								s_LibraryInstance = result;
								break;
							}
						}
					}

					return s_LibraryInstance;
				}
			}

			return 0;
		});
	}

	// ----------------------------------------------------------------------- //

	public static nint XML_ParserCreate(string? encoding = null)
	{
		var ptr = Marshal.StringToHGlobalAnsi(encoding);

		try
		{
			return __PInvoke(ptr);
		}
		finally
		{
			if (ptr != 0)
				Marshal.FreeHGlobal(ptr);
		}

		[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(XML_ParserCreate))]
		static extern nint __PInvoke(nint encoding = 0);
	}

	[DllImport(s_LibName)]
	public static extern void XML_ParserFree(nint parser);

	// ----------------------------------------------------------------------- //

	[DllImport(s_LibName)]
	public static extern XML_Status XML_StopParser(nint parser,
		[MarshalAs(UnmanagedType.I1)]
		bool resumable);

	[DllImport(s_LibName)]
	public static extern XML_Status XML_ResumeParser(nint parser);

	// ----------------------------------------------------------------------- //

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern long XML_GetCurrentLineNumber(nint parser);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern long XML_GetCurrentColumnNumber(nint parser);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentByteIndex(nint parser);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetCurrentByteCount(nint parser);

	// ----------------------------------------------------------------------- //

	static readonly Lazy<Dictionary<XML_Error, string>> s_ErrorToStringLazy = new(() =>
	{
		var dict = new Dictionary<XML_Error, string>
		{
			[0] = "none"
		};

		for (int i = 1; i <= 64; i++)
		{
			var code = (XML_Error)i;
			var msg = Marshal.PtrToStringAnsi(__PInvoke(code));

			if (msg == null)
				break;

			dict[(XML_Error)i] = msg;
		}

		return dict;

		[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(XML_ErrorString))]
		static extern nint __PInvoke(XML_Error code);

	}, true);

	public static string XML_ErrorString(XML_Error error)
		=> s_ErrorToStringLazy.Value.GetValueOrDefault(error) ?? error.ToString();

	[DllImport(s_LibName)]
	public static extern XML_Error XML_GetErrorCode(nint parser);

	public static string XML_ExpatVersion()
	{
		return Marshal.PtrToStringAnsi(__PInvoke())!;

		[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(XML_ExpatVersion))]
		static extern nint __PInvoke();
	}

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetSpecifiedAttributeCount(nint parser);

	public static XML_Status XML_Parse(nint parser, byte[] buf, int len, bool isFinal)
	{
		var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);

		try
		{
			return __PInvoke(parser, handle.AddrOfPinnedObject(), len, isFinal ? 1 : 0);
		}
		finally
		{
			handle.Free();
		}

		[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(XML_Parse))]
		static extern XML_Status __PInvoke(nint parser, nint buf, int len, int final);
	}
}

#pragma warning restore