using System.ComponentModel;
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
	/// Unmanaged parser handle.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public nint Handle => _parser;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="options">Parser configuration options.</param>
	public XmlParser(XmlParserOptions? options = default)
	{
		_options = options ?? XmlParserOptions.Default;

		_parser = XML_ParserCreate(_options.Encoding!.WebName);

		if (_parser == 0)
		{
			throw new ExpatException("Failed to create expat parser interface!")
			{
				Code = XmlError.NoMemory
			};
		}

		_userData = GCHandle.Alloc(this, GCHandleType.Normal);

		Reset(false);
	}

	void Reset(bool invokeNative)
	{
		if (invokeNative)
			XML_ParserReset(_parser, _options.Encoding.WebName);

		XML_SetUserData(_parser, (nint)_userData);
		XML_SetXmlDeclHandler(_parser, s_OnPrologCallback);
		XML_SetProcessingInstructionHandler(_parser, s_OnProcessingInstructionCallback);
		XML_SetCdataSectionHandler(_parser, s_OnCdataStartCallback, s_OnCdataEndCallback);
		XML_SetCharacterDataHandler(_parser, s_OnCharacterDataCallback);
		XML_SetCommentHandler(_parser, s_OnCommentCallback);
		XML_SetElementHandler(_parser, s_OnStartElementCallback, s_OnEndElementCallback);

		if (_options.HashSalt > 0)
			XML_SetHashSalt(_parser, _options.HashSalt);

		{
			if (_options.BillionLaughsAttackProtectionActivationThreshold is ulong value)
				XML_SetBillionLaughsAttackProtectionActivationThreshold(_parser, value);
		}

		{
			if (_options.BillionLaughsAttackProtectionMaximumAmplification is float value)
				XML_SetBillionLaughsAttackProtectionMaximumAmplification(_parser, value);
		}

		if (_options.EntityParsing.HasValue)
			_ = XML_SetParamEntityParsing(_parser, _options.EntityParsing.Value);
	}

	void ThrowIfDisposed()
		=> ObjectDisposedException.ThrowIf(_disposed, this);

	/// <summary>
	/// Reset the parser.
	/// </summary>
	public void Reset()
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			Reset(true);
		}
	}

	/// <summary>
	/// Suspend the parser.
	/// </summary>
	/// <param name="resumable">Determines whether the parser can be resumed later.</param>
	public void Suspend(bool resumable = true)
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			ThrowIfFailed(XML_StopParser(_parser, resumable));
		}
	}

	/// <summary>
	/// Resumes the parser.
	/// </summary>
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
				Code = code,
				LineNumber = _disposed ? 0 : XML_GetCurrentLineNumber(_parser),
				LinePosition = _disposed ? 0 : XML_GetCurrentColumnNumber(_parser),
				ByteIndex = _disposed ? 0 : XML_GetCurrentByteIndex(_parser),
				ByteCount = _disposed ? 0 : XML_GetCurrentByteCount(_parser),
			};

			throw exception;
		}
	}

	/// <summary>
	/// Parse some more of the document. 
	/// </summary>
	/// <param name="buf">A buffer containing part (or perhaps all) of the document.</param>
	/// <param name="len">The number of bytes of <paramref name="buf"/> that are part of the document.</param>
	/// <param name="isFinal">It informs the parser that this is the last piece of the document. Frequently, the last piece is empty (i.e. <paramref name="len"/> is zero)</param>
	/// <exception cref="ExpatException">An exception is thrown if there is any error in the parser.</exception>
	public void Parse(byte[] buf, int len, bool isFinal = false)
	{
		ThrowIfDisposed();

		lock (_syncRoot)
		{
			ThrowIfFailed(XML_Parse(_parser, buf, len, isFinal));
		}
	}

	/// <summary>
	/// Try parse some more of the document. The difference is that this function does not throw an exception if the expat returns an error.
	/// </summary>
	/// <param name="buf">A buffer containing part (or perhaps all) of the document.</param>
	/// <param name="len">The number of bytes of s that are part of the document.</param>
	/// <param name="isFinal">It informs the parser that this is the last piece of the document. Frequently, the last piece is empty (i.e. <paramref name="len"/> is zero)</param>
	/// <returns>A tuple containing whether the function was invoked successfully and the error code.</returns>
	public (bool Result, XmlError Error) TryParse(byte[] buf, int len, bool isFinal = false)
	{
		ThrowIfDisposed();

		lock (_syncRoot)
		{
			XmlError error = 0;

			var status = XML_Parse(_parser, buf, len, isFinal);

			if (status != XmlStatus.Success)
				error = XML_GetErrorCode(_parser);

			return new(status == XmlStatus.Success, error);
		}
	}

	/// <summary>
	/// Dispose parser and release allocated memory.
	/// </summary>
	public void Dispose()
	{
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