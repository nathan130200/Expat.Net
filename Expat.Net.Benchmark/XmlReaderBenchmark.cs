using System.Xml;
using BenchmarkDotNet.Attributes;

namespace Expat.Benchmark;

public class XmlReaderBenchmark
{
    [Params(false, true)]
    public bool StringAllocation { get; set; }

    byte[] _xmlBytes;

    [GlobalSetup(Target = nameof(Parsing_Buffered_InMemory))]
    public void OnGlobalSetup_For_Parsing_Buffered_InMemory()
    {
        _xmlBytes = File.ReadAllBytes("sample.xml");
    }

    [GlobalCleanup(Target = nameof(Parsing_Buffered_InMemory))]
    public void OnGlobalCleanup_For_Parsing_Buffered_InMemory()
    {
        _xmlBytes = null;
    }

    [Benchmark]
    public void Parsing_Buffered_InMemory()
    {
        using var ms = new MemoryStream(_xmlBytes, false);

        using var reader = XmlReader.Create(ms, new()
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreWhitespace = true
        });

        ConsumeParser(reader);
    }

    [Benchmark]
    public void Parsing_Buffered_FromFile()
    {
        using var fs = File.Open("sample.xml", FileMode.Open, FileAccess.Read, FileShare.Read);

        using var reader = XmlReader.Create(fs, new()
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreWhitespace = true
        });

        ConsumeParser(reader);
    }

    void ConsumeParser(XmlReader reader)
    {
        while (reader.Read())
        {
            if (StringAllocation)
                ConsumeTokens(reader);

            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (StringAllocation)
                        ConsumeTokens(reader);
                }
            }
        }
    }

    static void ConsumeTokens(XmlReader reader)
    {
        var localName = reader.LocalName;
        var prefix = reader.Prefix;
        var ns = reader.NamespaceURI;
        var value = reader.Value;
    }
}
