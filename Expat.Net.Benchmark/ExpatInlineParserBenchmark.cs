using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Expat.Benchmark;

public class ExpatInlineParserBenchmark
{
    byte[] _xmlBytes;

    [Params(false, true)]
    public bool StringAllocation { get; set; }

    [GlobalSetup]
    public void OnGlobalSetup()
    {
        _xmlBytes = File.ReadAllBytes("sample.xml");
    }

    [GlobalCleanup]
    public void OnGlobalCleanup()
    {
        _xmlBytes = null;
    }

    [Benchmark]
    public void Parsing_Bytes()
    {
        using var parser = new Expat.XmlParser();
        SetupParser(parser, StringAllocation);
        parser.Parse(_xmlBytes, true);
    }

    public static void SetupParser(XmlParser parser, bool listenForEvents)
    {
        if (listenForEvents)
        {
            parser.OnStartElement += (name, atts) =>
            {
                var name_ = name.ToUtf8String();

                foreach (var att in atts)
                {
                    var attName_ = att.Name.ToUtf8String();
                    var attValue_ = att.Value.ToUtf8String();
                }
            };

            parser.OnEndElement += name =>
            {
                var name_ = name.ToUtf8String();
            };

            parser.OnStartNamespaceDeclaration += (prefix, uri) =>
            {
                var prefix_ = prefix.ToUtf8String();
                var uri_ = uri.ToUtf8String();
            };

            parser.OnEndNamespaceDeclaration += (prefix) =>
            {
                var prefix_ = prefix.ToUtf8String();
            };

            parser.OnComment += value =>
            {
                var value_ = value.ToUtf8String();
            };

            parser.OnCdata += value =>
            {
                var value_ = value.ToUtf8String();
            };

            parser.OnText += value =>
            {
                var value_ = value.ToUtf8String();

            };

            parser.OnProcessingInstruction += (target, data) =>
            {
                var target_ = target.ToUtf8String();
                var data_ = data.ToUtf8String();
            };
        }
    }

    byte[] _contextBytes;

    [GlobalSetup(Target = nameof(Parsing_Buffered))]
    public void OnGlobalSetup_Parsing_Buffered()
    {
        _xmlBytes = File.ReadAllBytes("sample.xml");
        _contextBytes = ArrayPool<byte>.Shared.Rent(1024 * 4);
    }

    [GlobalCleanup(Target = nameof(Parsing_Buffered))]
    public void OnGlobalCleanup_Parsing_Buffered()
    {
        _xmlBytes = null;
        ArrayPool<byte>.Shared.Return(_contextBytes);
    }

    [Benchmark]
    public void Parsing_Buffered()
    {
        using var ms = new MemoryStream(_xmlBytes, false);

        using var parser = new XmlParser();

        SetupParser(parser, StringAllocation);

        while (true)
        {
            int len = ms.Read(_contextBytes);

            if (len == 0) break;

            parser.Parse(_contextBytes.AsSpan(..len), false);
        }
    }
}
