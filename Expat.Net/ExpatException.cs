namespace Expat;

public sealed class ExpatException : Exception
{
	public ExpatException()
	{

	}

	public ExpatException(string? message) : base(message)
	{

	}

	public XmlError Code { get; init; }
	public int LineNumber { get; init; }
	public int ColumnNumber { get; init; }
	public int ByteIndex { get; init; }
	public int ByteCount { get; init; }
	public string? Fragment { get; init; }
}