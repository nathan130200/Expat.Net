using System.Runtime.InteropServices;
using Expat.Native;

namespace Expat;

public delegate void PrologEventHandler(string version, string? encoding, XmlStandalone standalone);

public delegate void ProcessingInstructionEventHandler(string target, string? data);

public delegate void StartElementEventHandler(string name, IReadOnlyDictionary<string, string> attributes);

public delegate void EndElementEventHandler(string name);

public delegate void TextEventHandler(string value);

public delegate void CommentEventHandler(string value);

partial class XmlParser
{
	static XmlParser GetParser(nint userData)
	{
		if (GCHandle.FromIntPtr(userData).Target is XmlParser p)
			return p;

		throw new InvalidOperationException();
	}

	public event PrologEventHandler? OnProlog;

	public event ProcessingInstructionEventHandler? OnProcessingInstruction;

	public event StartElementEventHandler? OnStartElement;

	public event TextEventHandler? OnText;

	public event TextEventHandler? OnCdata;

	public event CommentEventHandler? OnComment;

	public event EndElementEventHandler? OnEndElement;

	static readonly XmlPrologHandler impl_PrologHandler = (userData, versionPtr, encodingPtr, standalone) =>
	{
		var parser = GetParser(userData);

		var handler = parser.OnProlog;

		if (handler == null)
			return;

		var version = Marshal.PtrToStringUTF8(versionPtr)!;

		var encoding = Marshal.PtrToStringUTF8(encodingPtr);

		handler(version, encoding, standalone);
	};

	static readonly XmlProcessingInstructionHandler impl_ProcessingInstructionHandler = (userData, targetPtr, dataPtr) =>
	{
		var parser = GetParser(userData);

		var handler = parser.OnProcessingInstruction;

		if (handler == null)
			return;

		var target = Marshal.PtrToStringUTF8(targetPtr)!;

		var data = Marshal.PtrToStringUTF8(dataPtr);

		handler(target, data);
	};

	static readonly XmlStartElementHandler impl_StartElementEventHandler = (userData, namePtr, attsPtr) =>
	{
		var parser = GetParser(userData);

		var handler = parser.OnStartElement;

		if (handler == null)
			return;

		var name = Marshal.PtrToStringUTF8(namePtr)!;

		var numAttributes = NativeMethods.XML_GetSpecifiedAttributeCount(parser._handle);

		var atts = new Dictionary<string, string>(numAttributes / 2);

		for (int i = 0; i < numAttributes; i += 2)
		{
			var attName = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(attsPtr, i * IntPtr.Size))!;

			var attValue = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(attsPtr, (i + 1) * IntPtr.Size))!;

			atts[attName] = attValue;
		}

		handler(name, atts);
	};

	static readonly XmlEndElementHandler impl_EndElementEventHandler = (userData, namePtr) =>
	{
		var parser = GetParser(userData);

		var handler = parser.OnEndElement;

		if (handler == null)
			return;

		handler(Marshal.PtrToStringUTF8(namePtr)!);
	};

	static readonly XmlStartCdataSectionHandler impl_StartCdataSectionHandler = (userData) =>
	{
		var parser = GetParser(userData);

		parser._inCdata = true;

		if (parser.OnCdata == null)
			return;

		parser._cdata ??= new();
	};

	static readonly XmlEndCdataSectionHandler impl_EndCdataSectionHandler = (userData) =>
	{
		var parser = GetParser(userData);

		var handler = parser.OnCdata;

		if (handler == null)
			return;

		parser._inCdata = false;

		handler(parser._cdata!.ToString());

		parser._cdata.Clear();
	};

	static readonly XmlCommentHandler impl_CommentHandler = (userData, dataPtr) =>
	{
		var parser = GetParser(userData);

		var handler = parser.OnComment;

		if (handler == null)
			return;

		handler(Marshal.PtrToStringUTF8(dataPtr)!);
	};

	static readonly unsafe XmlCharacterDataHandler impl_CharacterDataHandler = (userData, buf, len) =>
	{
		var parser = GetParser(userData);

		if (parser._inCdata)
		{
			if (parser._cdata == null)
				return;

			parser._cdata.Append(parser._encoding.GetString((byte*)buf, len));
		}
		else
		{
			var handler = parser.OnText;

			if (handler == null)
				return;

			handler(parser._encoding.GetString((byte*)buf, len));
		}
	};
}