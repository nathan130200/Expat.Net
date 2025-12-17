#pragma warning disable

#nullable enable

using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Expat.Net.Test")]

namespace Expat;

[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class PInvoke
{
	extension(XmlError error)
	{
		public string Message
		{
			get
			{
				return XML_ErrorString(error);
			}
		}
	}

	const string s_LibName = "expat";
	static nint s_LibraryInstance;
	static readonly Lock s_Lock = new();

	static readonly Lazy<IEnumerable<string>> s_LibraryFileNames = new(() =>
	{
		List<string> names = [];
		List<string> extensions = [];

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			extensions.Add(".dll");
		else
			extensions.AddRange(".so", ".so.1");

		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			extensions.Add(".dylib");

		foreach (var extension in extensions)
		{
#if DEBUG
			names.Add($"expatd{extension}");
			names.Add($"libexpatd{extension}");
#endif

			names.Add($"expat{extension}");
			names.Add($"libexpat{extension}");
		}

		return names;

	}, true);

	static PInvoke()
	{
		NativeLibrary.SetDllImportResolver(typeof(PInvoke).Assembly, static (libraryName, assembly, searchPaths) =>
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

			// fallback default library loader
			return (nint)0;
		});
	}

	// ----------------------------------------------------------------------- //

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern nint XML_ParserCreate(string? encoding);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_ParserReset(nint parser, string? encoding = null);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_SetUserData(nint parser, nint userData);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void XML_ParserFree(nint parser);

	// ----------------------------------------------------------------------- //

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_SetParamEntityParsing(nint parser, XmlEntityParsing parsing);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool XML_SetHashSalt(nint parser, ulong salt);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern bool XML_SetBillionLaughsAttackProtectionActivationThreshold(nint parser, ulong activationThresholdBytes);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern bool XML_SetBillionLaughsAttackProtectionMaximumAmplification(nint parser, float maximumAmplificationFactor);

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern XmlError XML_UseForeignDTD(nint parser, [MarshalAs(UnmanagedType.I1)] bool useDTD);

	// ----------------------------------------------------------------------- //

	[DllImport(s_LibName)]
	public static extern XmlStatus XML_StopParser(nint parser,
		[MarshalAs(UnmanagedType.I1)]
		bool resumable);

	[DllImport(s_LibName)]
	public static extern XmlStatus XML_ResumeParser(nint parser);

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

	static readonly Lazy<Dictionary<XmlError, string>> s_ErrorToStringLazy = new(() =>
	{
		var dict = new Dictionary<XmlError, string>
		{
			[0] = "none"
		};

		for (int i = 1; i <= 64; i++)
		{
			var code = (XmlError)i;
			var msg = Marshal.PtrToStringAnsi(__PInvoke(code));

			if (msg == null)
				break;

			dict[(XmlError)i] = msg;
		}

		return dict;

		[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(XML_ErrorString))]
		static extern nint __PInvoke(XmlError code);

	}, true);

	public static string XML_ErrorString(XmlError error)
		=> s_ErrorToStringLazy.Value.GetValueOrDefault(error) ?? error.ToString();

	[DllImport(s_LibName)]
	public static extern XmlError XML_GetErrorCode(nint parser);

	public static string XML_ExpatVersion()
	{
		return Marshal.PtrToStringAnsi(__PInvoke())!;

		[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(XML_ExpatVersion))]
		static extern nint __PInvoke();
	}

	[DllImport(s_LibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int XML_GetSpecifiedAttributeCount(nint parser);

	public static XmlStatus XML_Parse(nint parser, byte[] buf, int len, bool isFinal)
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
		static extern XmlStatus __PInvoke(nint parser, nint buf, int len, int final);
	}
}