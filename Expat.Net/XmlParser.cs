using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Expat.Native;

namespace Expat;

public sealed partial class XmlParser : IDisposable
{
	nint _handle;
	volatile bool _disposed;
	readonly GCHandle _userData;
	readonly XmlParserOptions _options;
	volatile bool _inCdata;
	StringBuilder? _cdata;
	Encoding _encoding;

	static Encoding GetEncoding(XmlEncoding type)
	{
		return type switch
		{
			XmlEncoding.Ascii => Encoding.ASCII,
			XmlEncoding.Latin1 => Encoding.Latin1,
			XmlEncoding.Utf16Le => Encoding.Unicode,
			XmlEncoding.Utf16Be => Encoding.BigEndianUnicode,
			XmlEncoding.Utf8 => Encoding.UTF8,
			_ => throw new InvalidOperationException("Unknown encoding type."),
		};
	}

	public XmlParser(XmlParserOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		_options = options;

		_encoding = GetEncoding(options.Encoding);

		_handle = NativeMethods.XML_ParserCreate(_encoding.WebName);

		if (_handle == 0)
			throw new OutOfMemoryException();

		_userData = GCHandle.Alloc(this);

		SetupParser(false);
	}

	unsafe void SetupParser(bool reset)
	{
		if (reset)
			NativeMethods.XML_ParserReset(_handle, _encoding.WebName);

		if (_options.HashSaltFactory != null)
		{
			Span<byte> buf = stackalloc byte[16];

			try
			{
				_options.HashSaltFactory(buf);

				fixed (byte* p = buf)
					NativeMethods.XML_SetHashSalt16Bytes(_handle, p);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(new InvalidOperationException("Uncaught exception while generating hash salt.", ex));
			}
		}

		if (_options.BillionLaughsAttackProtectionMaximumAmplification is float lolMaxAmp)
			NativeMethods.XML_SetBillionLaughsAttackProtectionMaximumAmplification(_handle, lolMaxAmp);

		if (_options.BillionLaughsAttackProtectionActivationThreshold is long lolMaxBytes)
			NativeMethods.XML_SetBillionLaughsAttackProtectionActivationThreshold(_handle, lolMaxBytes);

		if (_options.AllocTrackerMaximumAmplification is float allocMaxAmp)
			NativeMethods.XML_SetAllocTrackerMaximumAmplification(_handle, allocMaxAmp);

		if (_options.AllocTrackerActivationThreshold is long allocMaxBytes)
			NativeMethods.XML_SetAllocTrackerActivationThreshold(_handle, allocMaxBytes);

		if (_options.ParamEntityParsing is XmlParamEntityParsing pe)
			NativeMethods.XML_SetParamEntityParsing(_handle, pe);

		NativeMethods.XML_SetXmlDeclHandler(_handle, s_PrologHandlerImpl);

		NativeMethods.XML_SetProcessingInstructionHandler(_handle, s_ProcessingInstructionHandlerImpl);

		NativeMethods.XML_SetElementHandler(_handle, s_StartElementEventHandlerImpl, s_EndElementEventHandlerImpl);

		NativeMethods.XML_SetCdataSectionHandler(_handle, s_StartCdataSectionHandlerImpl, s_EndCdataSectionHandlerImpl);

		NativeMethods.XML_SetCommentHandler(_handle, s_CommentHandlerImpl);

		NativeMethods.XML_SetCharacterDataHandler(_handle, s_CharacterDataHandlerImpl);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;

			if (_handle != 0)
			{
				NativeMethods.XML_ParserFree(_handle);
				_handle = 0;
			}

			if (_userData.IsAllocated)
				_userData.Free();

			_encoding = default!;
		}
	}
}