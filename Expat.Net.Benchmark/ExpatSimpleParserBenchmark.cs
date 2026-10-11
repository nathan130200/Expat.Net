using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Expat.Benchmark;

public class ExpatSimpleParserBenchmark
{
    byte[] _xmlBytes, _inputBytes;

    [GlobalSetup]
    public void Init_Default()
    {
        _xmlBytes = File.ReadAllBytes("sample.xml");
    }

    [GlobalCleanup]
    public void Deinit_Default()
    {
        _xmlBytes = null;
    }

    [GlobalSetup(Target = nameof(Parse_Buffered))]
    public void Init_Buffered()
    {
        _inputBytes = ArrayPool<byte>.Shared.Rent(1024 * 4);
    }

    [GlobalCleanup(Target = nameof(Parse_Buffered))]
    public void Deinit_Buffered()
    {
        ArrayPool<byte>.Shared.Return(_inputBytes);
    }

    [Benchmark]
    public void Parse_Buffered()
    {
        using var fs = File.OpenRead("sample.xml");

        using var parser = new XmlParser();

        while (true)
        {
            int len = fs.Read(_inputBytes);

            if (len == 0) break;

            parser.Parse(_inputBytes.AsSpan(0, len), false);
        }
    }

    [Benchmark]
    public void Parse_InMemory()
    {
        using var parser = new XmlParser();
        parser.Parse(_xmlBytes, false);
    }
}
