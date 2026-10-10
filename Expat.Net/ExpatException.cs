namespace Expat;

public class ExpatException(string message) : ApplicationException(message)
{
    public XmlError Code { get; init; }

    public int LineNumber { get; init; }

    public int LinePosition { get; init; }

    public static ExpatException FromXmlError(XmlError code, int lineNum = default, int linePos = default)
    {
        var msg = NativeMethods.XML_ErrorString(code);

        return new ExpatException(msg)
        {
            Code = code,
            LineNumber = lineNum,
            LinePosition = linePos
        };
    }
}
