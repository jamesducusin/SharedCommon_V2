using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;

namespace SharedCommon.PerformanceTests;

[MemoryDiagnoser]
[SimpleJob]
public class CacheBenchmarks
{
    private string _key = null!;

    [GlobalSetup]
    public void Setup()
    {
        _key = "benchmark-key";
    }

    [Benchmark(Baseline = true)]
    public string BuildCacheKey_Concat() => "orders:order:" + _key;

    [Benchmark]
    public string BuildCacheKey_Interpolation() => $"orders:order:{_key}";

    [Benchmark]
    public string BuildCacheKey_Span()
    {
        Span<char> buffer = stackalloc char[64];
        var prefix = "orders:order:".AsSpan();
        prefix.CopyTo(buffer);
        _key.AsSpan().CopyTo(buffer[prefix.Length..]);
        return new string(buffer[..(prefix.Length + _key.Length)]);
    }
}

[MemoryDiagnoser]
[SimpleJob]
public class ResultPatternBenchmarks
{
    [Benchmark(Baseline = true)]
    public bool Exception_For_NotFound()
    {
        try
        {
            ThrowIfNotFound();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Benchmark]
    public bool Result_For_NotFound()
    {
        var result = ReturnResultIfNotFound();
        return result;
    }

    private static void ThrowIfNotFound() => throw new InvalidOperationException("Not found");

    private static bool ReturnResultIfNotFound() => false;
}

public class BenchmarkRunnerTests
{
    [Fact(Skip = "Run manually with: dotnet run -c Release")]
    public void Run_All_Benchmarks()
    {
        BenchmarkRunner.Run<CacheBenchmarks>();
        BenchmarkRunner.Run<ResultPatternBenchmarks>();
    }
}
