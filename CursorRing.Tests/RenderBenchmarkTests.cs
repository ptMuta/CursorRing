using System.Diagnostics;

namespace CursorRing.Tests;

public sealed class RenderBenchmarkTests
{
    [Fact]
    public void ResultCalculatesVisibleFrameStatistics()
    {
        var samples = new[]
        {
            Sample(100, 0, 40, 60, true),
            Sample(200, 10, 50, 70),
            Sample(300, 0, 60, 80),
            Sample(400, 30, 70, 90),
            new RenderBenchmarkSample(50, 5, default)
        };

        var result = RenderBenchmarkResult.Create(samples, 10d, 0);
        var microsecondsPerTick = 1_000_000d / Stopwatch.Frequency;

        Assert.Equal(5, result.TotalFrames);
        Assert.Equal(4, result.RenderedFrames);
        Assert.Equal(1, result.HiddenFrames);
        Assert.Equal(1, result.GcdActiveFrames);
        Assert.Contains("GCD-active 1", result.Format(), StringComparison.Ordinal);
        Assert.Equal(250d * microsecondsPerTick, result.MeanMicroseconds, 8);
        Assert.Equal(400d * microsecondsPerTick, result.P95Microseconds, 8);
        Assert.Equal(400d * microsecondsPerTick, result.P99Microseconds, 8);
        Assert.Equal(10d, result.MeanAllocatedBytes);
        Assert.Equal(30, result.MaxAllocatedBytes);
        Assert.Equal(5d, result.HiddenMeanAllocatedBytes);
        Assert.Equal(5, result.HiddenMaxAllocatedBytes);
        Assert.Equal(55d, result.MeanVertices);
        Assert.Equal(70, result.MaxVertices);
        Assert.Equal(75d, result.MeanIndices);
        Assert.Equal(90, result.MaxIndices);
    }

    [Fact]
    public void ResultHandlesNoVisibleFrames()
    {
        var samples = new[]
        {
            new RenderBenchmarkSample(100, 10, default),
            new RenderBenchmarkSample(200, 30, default)
        };

        var result = RenderBenchmarkResult.Create(samples, 10d, 0);

        Assert.Equal(0, result.RenderedFrames);
        Assert.Equal(0d, result.MeanMicroseconds);
        Assert.Equal(20d, result.HiddenMeanAllocatedBytes);
        Assert.Equal(30, result.HiddenMaxAllocatedBytes);
        Assert.Contains("0/2 visible frames", result.Format(), StringComparison.Ordinal);
    }

    [Fact]
    public void PercentilesUseNearestRank()
    {
        var samples = Enumerable.Range(1, 100).Select(value => Sample(value, 0, 1, 1)).ToArray();

        var result = RenderBenchmarkResult.Create(samples, 10d, 0);
        var microsecondsPerTick = 1_000_000d / Stopwatch.Frequency;

        Assert.Equal(95d * microsecondsPerTick, result.P95Microseconds, 8);
        Assert.Equal(99d * microsecondsPerTick, result.P99Microseconds, 8);
        Assert.Equal(result.MeanMicroseconds * 144d / 10_000d, result.MeanBudgetPercentAt144Hz, 8);
        Assert.Equal(result.P99Microseconds * 144d / 10_000d, result.P99BudgetPercentAt144Hz, 8);
    }

    [Fact]
    public void EmptyResultIsStable()
    {
        var result = RenderBenchmarkResult.Create([], 10d, 0);

        Assert.Equal(0, result.TotalFrames);
        Assert.Equal(0d, result.MeanMicroseconds);
        Assert.Equal(0d, result.HiddenMeanMicroseconds);
        Assert.Equal(0d, result.MeanAllocatedBytes);
    }

    [Fact]
    public void ResultSeparatesFailedFrames()
    {
        var samples = new[]
        {
            new RenderBenchmarkSample(100, 10, default),
            new RenderBenchmarkSample(200, 20, new RenderWork(RenderStatus.Failed, 0, 0, false))
        };

        var result = RenderBenchmarkResult.Create(samples, 10d, 0);

        Assert.Equal(1, result.HiddenFrames);
        Assert.Equal(1, result.FailedFrames);
        Assert.Equal(10d, result.HiddenMeanAllocatedBytes);
        Assert.Contains("failed 1", result.Format(), StringComparison.Ordinal);
    }

    [Fact]
    public void StartDoesNotRestartAnActiveRun()
    {
        var benchmark = new RenderBenchmark();
        var startedAt = 100L;
        var collectingAt = startedAt + (Stopwatch.Frequency * 3L);
        benchmark.Start(startedAt);

        benchmark.Start(startedAt + 1);

        Assert.Equal(BenchmarkPhase.Countdown, benchmark.Phase);
        Assert.Null(benchmark.Record(startedAt, 1, 0, default));
        Assert.Equal(0, benchmark.SampleCount);
        benchmark.Update(collectingAt - 1);
        Assert.Equal(BenchmarkPhase.Countdown, benchmark.Phase);
        benchmark.Update(collectingAt);
        Assert.True(benchmark.IsCollecting);
        benchmark.Record(collectingAt, 1, 0, default);
        Assert.Equal(1, benchmark.SampleCount);
        Assert.False(benchmark.GcdDetected);
        benchmark.Record(collectingAt, 1, 0, new RenderWork(RenderStatus.Rendered, 1, 1, true));
        Assert.True(benchmark.GcdDetected);
        benchmark.Cancel();
        Assert.False(benchmark.IsActive);
    }

    [Fact]
    public void BufferOverflowIsReportedAsUnsampled()
    {
        var benchmark = new RenderBenchmark();
        var startedAt = 100L;
        var collectingAt = startedAt + (Stopwatch.Frequency * 3L);
        benchmark.Start(startedAt);
        benchmark.Update(collectingAt);
        for (var index = 0; index < 30_000; index++)
        {
            benchmark.Record(collectingAt, 1, 0, default);
        }

        var result = benchmark.Record(collectingAt + (Stopwatch.Frequency * 10L), 1, 0, default);

        Assert.NotNull(result);
        Assert.Equal(30_001, result.Value.TotalFrames);
        Assert.Equal(1, result.Value.UnsampledFrames);
        Assert.Contains("unsampled 1", result.Value.Format(), StringComparison.Ordinal);
    }

    private static RenderBenchmarkSample Sample(long ticks, long allocatedBytes, int vertices, int indices, bool gcdActive = false)
    {
        return new RenderBenchmarkSample(ticks, allocatedBytes, new RenderWork(RenderStatus.Rendered, vertices, indices, gcdActive));
    }
}
