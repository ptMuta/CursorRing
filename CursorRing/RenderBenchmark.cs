#if CURSORRING_BENCHMARK
using System;
using System.Diagnostics;
using System.Globalization;

namespace CursorRing;

internal enum RenderStatus
{
    Hidden,
    Rendered,
    Failed
}

internal enum BenchmarkPhase
{
    Idle,
    Countdown,
    Collecting
}

internal readonly record struct RenderWork(RenderStatus Status, int Vertices, int Indices, bool GcdActive, bool CastSegmentsActive = false)
{
    internal bool Rendered => Status == RenderStatus.Rendered;
    internal bool Failed => Status == RenderStatus.Failed;
}

internal readonly record struct RenderBenchmarkSample(long Ticks, long AllocatedBytes, RenderWork Work);

internal readonly record struct RenderBenchmarkResult(
    double DurationSeconds,
    int TotalFrames,
    int RenderedFrames,
    int HiddenFrames,
    int FailedFrames,
    int GcdActiveFrames,
    int CastSegmentFrames,
    double MeanMicroseconds,
    double P95Microseconds,
    double P99Microseconds,
    double MaxMicroseconds,
    double HiddenMeanMicroseconds,
    double HiddenP95Microseconds,
    double MeanAllocatedBytes,
    long MaxAllocatedBytes,
    double HiddenMeanAllocatedBytes,
    long HiddenMaxAllocatedBytes,
    double MeanVertices,
    int MaxVertices,
    double MeanIndices,
    int MaxIndices,
    int UnsampledFrames)
{
    internal double MeanBudgetPercentAt144Hz => MeanMicroseconds * 144d / 10_000d;

    internal double P99BudgetPercentAt144Hz => P99Microseconds * 144d / 10_000d;

    internal static RenderBenchmarkResult Create(ReadOnlySpan<RenderBenchmarkSample> samples, double durationSeconds, int unsampledFrames)
    {
        var renderedFrames = 0;
        var hiddenFrames = 0;
        var failedFrames = 0;
        var gcdActiveFrames = 0;
        var castSegmentFrames = 0;
        long renderedTicks = 0;
        long hiddenTicks = 0;
        long allocatedBytes = 0;
        long maxAllocatedBytes = 0;
        long hiddenAllocatedBytes = 0;
        long hiddenMaxAllocatedBytes = 0;
        long vertices = 0;
        var maxVertices = 0;
        long indices = 0;
        var maxIndices = 0;

        foreach (var sample in samples)
        {
            if (sample.Work.Failed)
            {
                failedFrames++;
                continue;
            }

            if (!sample.Work.Rendered)
            {
                hiddenFrames++;
                hiddenTicks += sample.Ticks;
                hiddenAllocatedBytes += sample.AllocatedBytes;
                hiddenMaxAllocatedBytes = Math.Max(hiddenMaxAllocatedBytes, sample.AllocatedBytes);
                continue;
            }

            renderedFrames++;
            if (sample.Work.GcdActive)
            {
                gcdActiveFrames++;
            }
            if (sample.Work.CastSegmentsActive)
            {
                castSegmentFrames++;
            }

            renderedTicks += sample.Ticks;
            allocatedBytes += sample.AllocatedBytes;
            maxAllocatedBytes = Math.Max(maxAllocatedBytes, sample.AllocatedBytes);
            vertices += sample.Work.Vertices;
            maxVertices = Math.Max(maxVertices, sample.Work.Vertices);
            indices += sample.Work.Indices;
            maxIndices = Math.Max(maxIndices, sample.Work.Indices);
        }

        var renderedDurations = new long[renderedFrames];
        var hiddenDurations = new long[hiddenFrames];
        var renderedDurationIndex = 0;
        var hiddenDurationIndex = 0;
        foreach (var sample in samples)
        {
            if (sample.Work.Rendered)
            {
                renderedDurations[renderedDurationIndex++] = sample.Ticks;
            }
            else if (!sample.Work.Failed)
            {
                hiddenDurations[hiddenDurationIndex++] = sample.Ticks;
            }
        }

        Array.Sort(renderedDurations);
        Array.Sort(hiddenDurations);
        return new RenderBenchmarkResult(
            durationSeconds,
            samples.Length + unsampledFrames,
            renderedFrames,
            hiddenFrames,
            failedFrames,
            gcdActiveFrames,
            castSegmentFrames,
            ToMicroseconds(renderedTicks, renderedFrames),
            ToMicroseconds(Percentile(renderedDurations, 0.95d), 1),
            ToMicroseconds(Percentile(renderedDurations, 0.99d), 1),
            ToMicroseconds(renderedDurations.Length == 0 ? 0 : renderedDurations[^1], 1),
            ToMicroseconds(hiddenTicks, hiddenFrames),
            ToMicroseconds(Percentile(hiddenDurations, 0.95d), 1),
            Average(allocatedBytes, renderedFrames),
            maxAllocatedBytes,
            Average(hiddenAllocatedBytes, hiddenFrames),
            hiddenMaxAllocatedBytes,
            Average(vertices, renderedFrames),
            maxVertices,
            Average(indices, renderedFrames),
            maxIndices,
            unsampledFrames);
    }

    internal string Format()
    {
        if (RenderedFrames == 0)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"CursorRing benchmark: {DurationSeconds:F2} s, 0/{TotalFrames} visible frames, GCD-active 0, cast-segmented 0, hidden-path elapsed mean {HiddenMeanMicroseconds:F2} us, p95 {HiddenP95Microseconds:F2} us, allocations mean {HiddenMeanAllocatedBytes:F2} B/frame, max {HiddenMaxAllocatedBytes} B, failed {FailedFrames}, unsampled {UnsampledFrames}. Run it while the cursor ring is visible for render geometry.");
        }

        var hiddenSummary = HiddenFrames == 0
            ? string.Empty
            : string.Create(
                CultureInfo.InvariantCulture,
                $" Hidden path: {HiddenFrames} frames, elapsed mean {HiddenMeanMicroseconds:F2} us, p95 {HiddenP95Microseconds:F2} us, allocations mean {HiddenMeanAllocatedBytes:F2} B/frame, max {HiddenMaxAllocatedBytes} B.");
        return string.Create(
            CultureInfo.InvariantCulture,
            $"CursorRing benchmark: {DurationSeconds:F2} s, visible {RenderedFrames}/{TotalFrames}, GCD-active {GcdActiveFrames}, cast-segmented {CastSegmentFrames}, render-path elapsed mean {MeanMicroseconds:F2} us, p95 {P95Microseconds:F2} us, p99 {P99Microseconds:F2} us, max {MaxMicroseconds:F2} us, allocations mean {MeanAllocatedBytes:F2} B/frame, max {MaxAllocatedBytes} B, geometry mean {MeanVertices:F1} vertices/{MeanIndices:F1} indices, max {MaxVertices}/{MaxIndices}, mean 144 Hz budget share {MeanBudgetPercentAt144Hz:F3}%, p99 share {P99BudgetPercentAt144Hz:F3}%, failed {FailedFrames}, unsampled {UnsampledFrames}.{hiddenSummary}");
    }

    private static long Percentile(long[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0)
        {
            return 0;
        }

        var index = Math.Clamp((int)Math.Ceiling(sortedValues.Length * percentile) - 1, 0, sortedValues.Length - 1);
        return sortedValues[index];
    }

    private static double ToMicroseconds(long ticks, int count)
    {
        return count == 0 ? 0d : ticks * 1_000_000d / Stopwatch.Frequency / count;
    }

    private static double Average(long total, int count)
    {
        return count == 0 ? 0d : (double)total / count;
    }
}

internal sealed class RenderBenchmark
{
    private const int SampleCapacity = 30_000;
    private const double BenchmarkSeconds = 10d;
    private const double CountdownSeconds = 3d;
    private RenderBenchmarkSample[]? samples;
    private int sampleCount;
    private int unsampledFrames;
    private long startedAt;
    private long countdownEndsAt;
    private long endsAt;

    internal BenchmarkPhase Phase { get; private set; }

    internal bool IsActive => Phase != BenchmarkPhase.Idle;

    internal bool IsCollecting => Phase == BenchmarkPhase.Collecting;

    internal int SampleCount => sampleCount + unsampledFrames;

    internal bool GcdDetected { get; private set; }

    internal bool CastSegmentsDetected { get; private set; }

    internal double Progress => IsCollecting
        ? Math.Clamp(Stopwatch.GetElapsedTime(startedAt).TotalSeconds / BenchmarkSeconds, 0d, 1d)
        : 1d;

    internal int CountdownSecondsRemaining => Phase == BenchmarkPhase.Countdown
        ? Math.Max(1, (int)Math.Ceiling(Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), countdownEndsAt).TotalSeconds))
        : 0;

    internal double CountdownProgress => Phase == BenchmarkPhase.Countdown
        ? Math.Clamp(1d - (Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), countdownEndsAt).TotalSeconds / CountdownSeconds), 0d, 1d)
        : 1d;

    internal RenderBenchmarkResult? LastResult { get; private set; }

    internal void Start()
    {
        Start(Stopwatch.GetTimestamp());
    }

    internal void Start(long timestamp)
    {
        if (IsActive)
        {
            return;
        }

        samples = new RenderBenchmarkSample[SampleCapacity];
        sampleCount = 0;
        unsampledFrames = 0;
        GcdDetected = false;
        CastSegmentsDetected = false;
        countdownEndsAt = timestamp + (long)(Stopwatch.Frequency * CountdownSeconds);
        Phase = BenchmarkPhase.Countdown;
        LastResult = null;
    }

    internal void Update(long timestamp)
    {
        if (Phase != BenchmarkPhase.Countdown || timestamp < countdownEndsAt)
        {
            return;
        }

        startedAt = timestamp;
        endsAt = startedAt + (long)(Stopwatch.Frequency * BenchmarkSeconds);
        Phase = BenchmarkPhase.Collecting;
    }

    internal void Cancel()
    {
        samples = null;
        Phase = BenchmarkPhase.Idle;
    }

    internal RenderBenchmarkResult? Record(long timestamp, long ticks, long allocatedBytes, RenderWork work)
    {
        if (!IsCollecting || samples is null)
        {
            return null;
        }

        GcdDetected |= work.GcdActive;
        CastSegmentsDetected |= work.CastSegmentsActive;
        if (sampleCount < samples.Length)
        {
            samples[sampleCount++] = new RenderBenchmarkSample(ticks, Math.Max(0, allocatedBytes), work);
        }
        else
        {
            unsampledFrames++;
        }

        if (timestamp < endsAt)
        {
            return null;
        }

        var result = RenderBenchmarkResult.Create(samples.AsSpan(0, sampleCount), Stopwatch.GetElapsedTime(startedAt, timestamp).TotalSeconds, unsampledFrames);
        samples = null;
        Phase = BenchmarkPhase.Idle;
        LastResult = result;
        return result;
    }
}
#endif
