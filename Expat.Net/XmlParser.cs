using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static Expat.PInvoke;

namespace Expat;

/// <summary>
/// Represents the unmanaged expat XML parser wrapper.
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
	/// <param name="options">Parser init options.</param>
	public XmlParser(XmlParserOptions? options = default)
	{
		_options = options ?? XmlParserOptions.Default;

		_parser = XML_ParserCreate(_options.Encoding.WebName.ToUpper());

		Debug.Assert(_parser != 0);

		if (_parser == 0)
		{
			throw new ExpatException("Failed to create native expat parser interface!")
			{
				Code = XmlError.NoMemory
			};
		}

		_userData = GCHandle.Alloc(this, GCHandleType.Normal);

		Reset(false);
	}

	public XmlError GetLastError()
	{
		if (_disposed)
			return XmlError.UnexpectedState;

		return XML_GetErrorCode(_parser);
	}

	void Reset(bool invokeNative)
	{
		if (invokeNative)
			if (!XML_ParserReset(_parser, _options.Encoding.WebName.ToUpper()))
				Trace.WriteLine($"XML_ParserReset(0x{_parser:x8}, {_options.Encoding.WebName}) failed. (code: {GetLastError()})");

		XML_SetUserData(_parser, (nint)_userData);
		XML_SetXmlDeclHandler(_parser, s_OnPrologCallback);
		XML_SetProcessingInstructionHandler(_parser, s_OnProcessingInstructionCallback);
		XML_SetCdataSectionHandler(_parser, s_OnCdataStartCallback, s_OnCdataEndCallback);
		XML_SetCharacterDataHandler(_parser, s_OnCharacterDataCallback);
		XML_SetCommentHandler(_parser, s_OnCommentCallback);
		XML_SetElementHandler(_parser, s_OnStartElementCallback, s_OnEndElementCallback);

		{
			if (_options.HashSalt is ulong value)
			{
				if (value == 0)
					value = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));

				if (!XML_SetHashSalt(_parser, value))
					Trace.WriteLine($"XML_SetHashSalt(0x{(int)_parser:x8}, {value}) failed.");
			}
		}

		{
			if (_options.BillionLaughsAttackProtectionActivationThreshold is long value && value > 0)
				if (!XML_SetBillionLaughsAttackProtectionActivationThreshold(_parser, value))
					Trace.WriteLine($"XML_SetBillionLaughsAttackProtectionActivationThreshold(0x{_parser:x8}, {value}) failed.");
		}

		{
			if (_options.BillionLaughsAttackProtectionMaximumAmplification is float value)
				if (!XML_SetBillionLaughsAttackProtectionMaximumAmplification(_parser, value))
					Trace.WriteLine($"XML_SetBillionLaughsAttackProtectionMaximumAmplification(0x{_parser:x8}, {value}) failed.");
		}

		{
			if (_options.EntityParsing is XmlEntityParsing value)
				if (!XML_SetParamEntityParsing(_parser, value))
					Trace.WriteLine($"XML_SetParamEntityParsing(0x{_parser:x8}, {value}) failed.");
		}

		{
			if (_options.UseForeignDTD is bool value)
			{
				var result = XML_UseForeignDTD(_parser, value);

				if (result != XmlError.None)
				{
					Trace.WriteLine($"XML_UseForeignDTD(0x{_parser:x8}, {value}) failed. (code: {result})");

					if (result != XmlError.FeatureRequiresXmlDtd)
						ThrowException(result);
				}
			}
		}
	}

	void ThrowIfDisposed()
		=> ObjectDisposedException.ThrowIf(_disposed, this);

	/// <summary>
	/// Clean up the memory structures maintained by the parser so that it may be used again.
	/// After this has been called, the parser is ready to start parsing a new document. 
	/// </summary>
	/// <exception cref="ObjectDisposedException">If the parser has been disposed.</exception>
	public void Reset()
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			Reset(true);
		}
	}

	/// <summary>
	/// Stops parsing. Some call-backs may still follow because they would otherwise get lost, including
	/// <list type="bullet">
	/// <item>the end element handler for empty elements when stopped in the start element handler</item>
	/// <item>the end namespace declaration handler when stopped in the end element handler</item>
	/// <item>the character data handler when stopped in the character data handler while making multiple call-backs on a contiguous chunk of characters</item>
	/// </list>
	/// </summary>
	/// <param name="resumable">Determines whether the parser can be resumed later.</param>
	/// <exception cref="ObjectDisposedException">If the parser has been disposed.</exception>
	/// <exception cref="ExpatException">
	/// Throws if any conditions are met:
	/// <list type="bullet">
	/// <item>when stopping or suspending a parser before it has started</item>
	/// <item>when suspending an already suspended parser</item>
	/// <item>when the parser has already finished</item>
	/// <item>when suspending while parsing an external PE</item>
	/// </list>
	/// </exception>
	public void Suspend(bool resumable = true)
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			ThrowIfFailed(XML_StopParser(_parser, resumable));
		}
	}

	/// <summary>
	/// Resumes parsing after it has been suspended with <see cref="Suspend"/>
	/// </summary>
	/// <exception cref="ObjectDisposedException">If the parser has been disposed.</exception>
	/// <exception cref="ExpatException">Throws if the parser was not currently suspended.</exception>
	public void Resume()
	{
		lock (_syncRoot)
		{
			ThrowIfDisposed();
			ThrowIfFailed(XML_ResumeParser(_parser));
		}
	}

	void ThrowIfFailed(XmlStatus status, [CallerArgumentExpression(nameof(status))] string? expression = default)
	{
		if (status != XmlStatus.Success)
		{
			var code = _disposed
				? XmlError.UnexpectedState
				: XML_GetErrorCode(_parser);

			ThrowException(code, expression);
		}
	}

	void ThrowException(XmlError error, string? expression = null)
	{
		var sb = new StringBuilder();

		if (expression != null)
		{
			var methodNameEnd = expression.IndexOf('(');

			if (methodNameEnd != -1)
				expression = $"{expression[0..methodNameEnd]}(0x{_parser:x8})";

			sb.Append($"{expression} = {error}: ");
		}

		sb.Append(XML_ErrorString(error));

		var exception = new ExpatException(sb.ToString())
		{
			Code = error,
			LineNumber = _disposed ? 0 : XML_GetCurrentLineNumber(_parser),
			LinePosition = _disposed ? 0 : XML_GetCurrentColumnNumber(_parser),
			ByteIndex = _disposed ? 0 : XML_GetCurrentByteIndex(_parser),
			ByteCount = _disposed ? 0 : XML_GetCurrentByteCount(_parser),
		};

		Trace.WriteLine(exception);

		throw exception;
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