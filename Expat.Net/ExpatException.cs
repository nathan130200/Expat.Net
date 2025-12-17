namespace Expat;

public sealed class ExpatException : Exception
{
	internal ExpatException()
	{

	}

	internal ExpatException(string message) : base(message)
	{

	}

	internal ExpatException(string message, Exception innerException) : base(message, innerException)
	{

	}

	public XmlError Code { get; init; }
	public int ByteIndex { get; init; }
	public int ByteCount { get; init; }
	public long LineNumber { get; init; }
	public long LinePosition { get; init; }
}