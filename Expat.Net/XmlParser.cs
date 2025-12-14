using System.Runtime.InteropServices;
using System.Text;
using static Expat.Native;

namespace Expat;

public sealed class XmlParser : IDisposable
{
	nint _parser;
	Encoding? _encoding;
	volatile bool _disposed;
	volatile bool _isCdataSection;
	StringBuilder? _cdataSection;
	readonly GCHandle _userData;
	readonly Lock _syncRoot = new();

	public XmlParser(Encoding? encoding = default)
	{
		_encoding = encoding ?? Encoding.UTF8;
		_parser = XML_ParserCreate(_encoding.WebName);
		_userData = GCHandle.Alloc(this);

		Setup(false);
	}

	void Setup(bool reset)
	{

	}

	void ThrowIfDisposed()
		=> ObjectDisposedException.ThrowIf(_disposed, this);

	public void Reset()
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			Setup(true);
		}
	}

	public void Suspend(bool resumable)
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			ThrowIfFailed(XML_StopParser(_parser, resumable));
		}
	}

	public void Resume()
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			ThrowIfFailed(XML_ResumeParser(_parser));
		}
	}

	void ThrowIfFailed(XML_Status status)
	{
		if (status != XML_Status.XML_STATUS_OK)
		{
			var code = _disposed ? XML_Error.XML_ERROR_UNEXPECTED_STATE
				: XML_GetErrorCode(_parser);

			var exception = new ExpatException(XML_ErrorString(code))
			{
				Data =
				{
					["Code"] = code,
					["LineNumber"] = _disposed ? 0 : XML_GetCurrentLineNumber(_parser),
					["ColumnNumber"] = _disposed ? 0 : XML_GetCurrentColumnNumber(_parser),
					["ByteIndex"] = _disposed ? 0 : XML_GetCurrentByteIndex(_parser),
					["ByteCount"] = _disposed ? 0 : XML_GetCurrentByteCount(_parser),
				}
			};

			throw exception;
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		_encoding = null;

		_isCdataSection = false;

		_cdataSection?.Clear();
		_cdataSection = null;

		if (_userData.IsAllocated)
			_userData.Free();

		if (_parser != 0)
		{
			XML_ParserFree(_parser);
			_parser = 0;
		}
	}
}
