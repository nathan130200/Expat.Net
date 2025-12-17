using System.Runtime.InteropServices;
using System.Text;

namespace Expat;

partial class XmlParser
{
	/// <summary>
	/// Event invoked when start tag is parsed.
	/// </summary>
	public event Action<string, IReadOnlyDictionary<string, string>>? OnStartTag;

	/// <summary>
	/// Event invoked when end tag is parsed.
	/// </summary>
	public event Action<string>? OnEndTag;

	/// <summary>
	/// Event invoked when text node is parsed.
	/// </summary>
	public event Action<string>? OnText;

	/// <summary>
	/// Event invoked when cdata node is parsed.
	/// </summary>
	public event Action<string>? OnCdata;

	/// <summary>
	/// Event invoked when comment node is parsed.
	/// </summary>
	public event Action<string>? OnComment;

	/// <summary>
	/// Event invoked when processing instruction is parsed.
	/// </summary>
	public event Action<string, string>? OnProcessingInstruction;

	/// <summary>
	/// Gets the managed parser state from callback.
	/// </summary>
	/// <param name="userData">Pointer to an <see cref="GCHandle"/> wrapping our managed <see cref="XmlParser"/> instance</param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException">Callback was invoked but managed parser state is invalid.</exception>
	protected static XmlParser GetParserState(nint userData)
	{
		var result = GCHandle.FromIntPtr(userData);

		if (!result.IsAllocated)
			throw new InvalidOperationException("Unmanaged callback invoked without managed XML parser state.");

		return (XmlParser)result.Target!;
	}

	static readonly XML_StartElementHandler s_OnStartElementCallback = static (userData, name_, attrs_) =>
	{
		var context = GetParserState(userData);

		if (context.OnStartTag == null)
			return;

		var tagName = Marshal.PtrToStringAnsi(name_)!;

		var attributes = new Dictionary<string, string>();

		{
			var count = PInvoke.XML_GetSpecifiedAttributeCount(context._parser);

			if (count > 0)
			{
				// name,value,name,value,...

				for (int i = 0; i < count; i += 2)
				{
					var attName = Marshal.ReadIntPtr(attrs_, i * nint.Size);
					var attVal = Marshal.ReadIntPtr(attrs_, (i + 1) * nint.Size);
					attributes[Marshal.PtrToStringAnsi(attName)!] = Marshal.PtrToStringAnsi(attVal)!;
				}
			}
		}

		context.OnStartTag(tagName, attributes);
	};

	static readonly XML_EndElementHandler s_OnEndElementCallback = static (userData, name) =>
	{
		var context = GetParserState(userData);

		context.OnEndTag?.Invoke(Marshal.PtrToStringAnsi(name)!);
	};


	/// <summary>
	/// Safely decode an sized string.
	/// </summary>
	/// <param name="enc">Encoding used by XML parser to decode string from pointer.</param>
	/// <param name="ptr">Pointer to native string</param>
	/// <param name="size">Size of native string (when its non C-Style string)</param>
	/// <returns></returns>
	protected static unsafe string DecodeString(Encoding enc, nint ptr, int size)
		=> enc.GetString((byte*)ptr, size);

	static readonly XML_CharacterDataHandler s_OnCharacterDataCallback = static (userData, buf, len) =>
	{
		var context = GetParserState(userData);

		if (context._isCdataSection)
		{
			if (context.OnCdata == null)
				return;

			var str = DecodeString(context._options.Encoding, buf, len);
			context._cdataSection!.Append(str);
		}
		else
		{
			if (context.OnText == null)
				return;

			context.OnText(DecodeString(context._options.Encoding, buf, len));
		}
	};

	static readonly XML_CdataSectionHandler s_OnCdataStartCallback = static (userData) =>
	{
		var context = GetParserState(userData);

		context._isCdataSection = true;

		if (context.OnCdata == null)
			return;

		context._cdataSection ??= new();
	};

	static readonly XML_CdataSectionHandler s_OnCdataEndCallback = static (userData) =>
	{
		var context = GetParserState(userData);

		if (context.OnCdata == null)
			return;

		var buf = context._cdataSection!.ToString();
		context._cdataSection.Clear();
		context.OnCdata(buf);
	};

	static readonly XML_CommentHandler s_OnCommentCallback = static (userData, data) =>
	{
		var context = GetParserState(userData);

		context.OnComment?.Invoke(Marshal.PtrToStringAnsi(data)!);
	};

	static readonly XML_ProcessingInstructionHandler s_OnProcessingInstructionCallback = static (userData, target, data) =>
	{
		var context = GetParserState(userData);

		context.OnProcessingInstruction?.Invoke(
			Marshal.PtrToStringAnsi(target)!,
			Marshal.PtrToStringAnsi(data)!
		);
	};
}
