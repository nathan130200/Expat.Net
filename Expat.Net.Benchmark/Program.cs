using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Expat.Benchmark;

public static class Program
{
    static void Main(string[] args)
    {
#if DEBUG
        Environment.FailFast("Should not run benchmark in Debug mode!");
#endif

        Trace.Listeners.Add(new ConsoleTraceListener());

        {
            var path = string.Concat(Environment.GetEnvironmentVariable("PATH"), Path.PathSeparator, Directory.GetCurrentDirectory());

            Environment.SetEnvironmentVariable("PATH", path);
        }

        var config = ManualConfig.CreateMinimumViable()

            .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()))

            .AddExporter(new HtmlExporter())

            .WithOptions(ConfigOptions.JoinSummary | ConfigOptions.DisableLogFile)

            .AddJob(Job.ShortRun);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll(config, args: args);
    }
}

public static class ReadOnlySpanHelpers
{
    public static string ToUtf8String(this ReadOnlySpan<byte> span) => Encoding.UTF8.GetString(span);
}