using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Expat.Benchmark;

public class Expat_RecycleParser_Benchmark
{
    byte[] _xmlBytes;
    XmlParser _parser;

    [Params(false, true)]
    public bool StringAllocation { get; set; }

    [GlobalSetup]
    public void OnGlobalSetup()
    {
        _parser = new();
        Expat_InlineParser_Benchmark.SetupParser(_parser, StringAllocation);
        _xmlBytes = File.ReadAllBytes("sample.xml");
    }

    [GlobalCleanup]
    public void OnGlobalCleanup()
    {
        _parser.Dispose();
        _xmlBytes = null;
    }

    [Benchmark]
    public void Parsing_Bytes()
    {
        _parser.Reset();

        _parser.Parse(_xmlBytes, false);
    }

    byte[] _contextBytes;

    [GlobalSetup(Target = nameof(Parsing_Buffered))]
    public void OnGlobalSetup_Parsing_Buffered()
    {
        _parser = new();
        Expat_InlineParser_Benchmark.SetupParser(_parser, StringAllocation);
        _xmlBytes = File.ReadAllBytes("sample.xml");
        _contextBytes = ArrayPool<byte>.Shared.Rent(1024 * 4);
    }

    [GlobalCleanup(Target = nameof(Parsing_Buffered))]
    public void OnGlobalCleanup_Parsing_Buffered()
    {
        _xmlBytes = null;
        _parser.Dispose();
        ArrayPool<byte>.Shared.Return(_contextBytes);
    }

    [Benchmark]
    public void Parsing_Buffered()
    {
        _parser.Reset();

        using var ms = new MemoryStream(_xmlBytes, false);

        while (true)
        {
            int len = ms.Read(_contextBytes);

            if (len == 0) break;

            _parser.Parse(_contextBytes.AsSpan(0, len), false);
        }
    }
}