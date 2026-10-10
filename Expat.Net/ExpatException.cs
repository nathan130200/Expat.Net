namespace Expat;

public class ExpatException : ApplicationException
{
    ExpatException(string message) : base(message)
    {

    }

    public XmlError Code { get; init; }

    public int LineNumber { get; init; }

    public int LinePosition { get; init; }

    public static ExpatException Create(XmlError code, int lineNum = default, int linePos = default)
    {
        var msg = NativeMethods.XML_ErrorString(code) ?? $"XML parser error (code: {(int)code})";

        return new ExpatException(msg)
        {
            Code = code,
            LineNumber = lineNum,
            LinePosition = linePos
        };
    }
}
