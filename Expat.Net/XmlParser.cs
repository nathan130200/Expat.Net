using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static Expat.PInvoke;

namespace Expat;

public sealed class XmlParser : IDisposable
{
	nint _parser;
	volatile bool _disposed;
	volatile bool _isCdataSection;
	StringBuilder? _cdataSection;
	readonly GCHandle<XmlParser> _userData;
	readonly Lock _syncRoot = new();
	XmlParserOptions? _options;

	public XmlParser(XmlParserOptions? options = default)
	{
		_options = options ?? XmlParserOptions.Default;

		if (_options.MemoryHandlingSuite == null)
			_parser = XML_ParserCreate(_options.Encoding!.WebName);
		else
			_parser = XML_ParserCreate_MM(_options.Encoding!.WebName,
				ref _options.MemoryHandlingSuite.__native, null);

		Debug.Assert(_parser != 0, "out of memory");

		_userData = new(this);

		if (_options!.HashSalt is ulong value)
		{
			if (value == 0)
			{
				Span<byte> buf = stackalloc byte[8];
				Random.Shared.NextBytes(buf);
				value = BitConverter.ToUInt64(buf);
			}

			XML_SetHashSalt(_parser, value);
		}

		Init();
	}

	void Init(bool reset = false)
	{
		if (reset)
			XML_ParserReset(_parser, _options!.Encoding!.WebName);

		XML_SetUserData(_parser, GCHandle<XmlParser>.ToIntPtr(_userData));
	}

	void ThrowIfDisposed()
		=> ObjectDisposedException.ThrowIf(_disposed, this);

	public void Reset()
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			Init(true);
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

	void ThrowIfFailed(XmlStatus status)
	{
		if (status != XmlStatus.Success)
		{
			var code = _disposed ? XmlError.UnexpectedState
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

		_options = null;

		_isCdataSection = false;

		_cdataSection?.Clear();
		_cdataSection = null;

		if (_userData.IsAllocated)
			_userData.Dispose();

		if (_parser != 0)
		{
			XML_ParserFree(_parser);
			_parser = 0;
		}
	}
}
