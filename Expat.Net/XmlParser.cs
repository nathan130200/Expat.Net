using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static Expat.PInvoke;

namespace Expat;

/// <summary>
/// Represents the expat xml parser wrapper class.
/// </summary>
public sealed partial class XmlParser : IDisposable
{
	nint _parser;
	volatile bool _disposed;
	volatile bool _isCdataSection;
	StringBuilder? _cdataSection;
	readonly GCHandle _userData;
	XmlParserOptions _options;
	readonly Lock _syncRoot = new();

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="options">Parser configuration options.</param>
	public XmlParser(XmlParserOptions? options = default)
	{
		_options = options ?? XmlParserOptions.Default;

		_parser = XML_ParserCreate(_options.Encoding!.WebName);

		Debug.Assert(_parser != 0, "out of memory");

		_userData = GCHandle.Alloc(this, GCHandleType.Normal);

		Reset(false);
	}

	void Reset(bool invokeNative)
	{
		if (invokeNative)
			XML_ParserReset(_parser, _options!.Encoding!.WebName);

		XML_SetUserData(_parser, (nint)_userData);
		XML_SetElementHandler(_parser, s_OnStartElementCallback, s_OnEndElementCallback);
		XML_SetCdataSectionHandler(_parser, s_OnCdataStartCallback, s_OnCdataEndCallback);
		XML_SetCharacterDataHandler(_parser, s_OnCharacterDataCallback);
		XML_SetCommentHandler(_parser, s_OnCommentCallback);
		XML_SetProcessingInstructionHandler(_parser, s_OnProcessingInstructionCallback);

		if (_options!.HashSalt > 0)
			XML_SetHashSalt(_parser, _options.HashSalt);

		{
			if (_options.BillionLaughsAttackProtectionActivationThreshold is ulong value)
				XML_SetBillionLaughsAttackProtectionActivationThreshold(_parser, value);
		}

		{
			if (_options.BillionLaughsAttackProtectionMaximumAmplification is float value)
				XML_SetBillionLaughsAttackProtectionMaximumAmplification(_parser, value);
		}
	}

	void ThrowIfDisposed()
		=> ObjectDisposedException.ThrowIf(_disposed, this);

	public void Reset()
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			Reset(true);
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
			var code = _disposed
				? XmlError.UnexpectedState
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

	public void Parse(byte[] buf, int len)
	{
		ThrowIfDisposed();

		lock (_syncRoot)
		{
			ThrowIfFailed(XML_Parse(_parser, buf, len, len <= 0));
		}
	}

	public bool TryParse(byte[] buf, int len, out XmlError error)
	{
		ThrowIfDisposed();

		lock (_syncRoot)
		{
			error = 0;

			var status = XML_Parse(_parser, buf, len, len <= 0);

			if (status != XmlStatus.Success)
				error = XML_GetErrorCode(_parser);

			return status == XmlStatus.Success;
		}
	}

	public void Dispose()
	{
		// better synchronize here. will prevent early
		// deletion of userData and invalidating unmanaged
		// parser too early.

		lock (_syncRoot)
		{
			if (_disposed)
				return;

			_disposed = true;

			_options = null!;

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
}
