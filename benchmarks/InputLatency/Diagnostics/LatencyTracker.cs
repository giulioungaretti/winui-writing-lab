using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace win2dlowlatecny.Diagnostics;

/// <summary>
/// Tracks input-to-render latency for performance monitoring.
/// </summary>
public sealed class LatencyTracker
{
    private readonly Stopwatch _stopwatch;
    private readonly Queue<double> _recentLatencies;
    private readonly object _lock = new();

    private const int MaxSamples = 100;

    /// <summary>
    /// Current average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; private set; }

    /// <summary>
    /// Minimum latency observed in recent samples.
    /// </summary>
    public double MinLatencyMs { get; private set; }

    /// <summary>
    /// Maximum latency observed in recent samples.
    /// </summary>
    public double MaxLatencyMs { get; private set; }

    /// <summary>
    /// Last recorded latency in milliseconds.
    /// </summary>
    public double LastLatencyMs { get; private set; }

    /// <summary>
    /// Total number of samples recorded.
    /// </summary>
    public long TotalSamples { get; private set; }

    /// <summary>
    /// Stopwatch frequency for converting ticks to milliseconds.
    /// </summary>
    public static long Frequency => Stopwatch.Frequency;

    public LatencyTracker()
    {
        _stopwatch = Stopwatch.StartNew();
        _recentLatencies = new Queue<double>(MaxSamples);
        MinLatencyMs = double.MaxValue;
        MaxLatencyMs = 0;
    }

    /// <summary>
    /// Get the current timestamp in ticks.
    /// </summary>
    public long GetTimestamp()
    {
        return _stopwatch.ElapsedTicks;
    }

    /// <summary>
    /// Convert ticks to milliseconds.
    /// </summary>
    public static double TicksToMs(long ticks)
    {
        return (double)ticks / Stopwatch.Frequency * 1000.0;
    }

    /// <summary>
    /// Record a latency sample from input timestamp to current time.
    /// </summary>
    public void RecordLatency(long inputTimestampTicks)
    {
        long currentTicks = _stopwatch.ElapsedTicks;
        long latencyTicks = currentTicks - inputTimestampTicks;
        double latencyMs = TicksToMs(latencyTicks);

        RecordLatencyMs(latencyMs);
    }

    /// <summary>
    /// Record a latency sample directly in milliseconds.
    /// </summary>
    public void RecordLatencyMs(double latencyMs)
    {
        lock (_lock)
        {
            LastLatencyMs = latencyMs;
            TotalSamples++;

            _recentLatencies.Enqueue(latencyMs);
            if (_recentLatencies.Count > MaxSamples)
            {
                _recentLatencies.Dequeue();
            }

            // Recalculate statistics
            double sum = 0;
            double min = double.MaxValue;
            double max = 0;

            foreach (var sample in _recentLatencies)
            {
                sum += sample;
                if (sample < min) min = sample;
                if (sample > max) max = sample;
            }

            AverageLatencyMs = sum / _recentLatencies.Count;
            MinLatencyMs = min;
            MaxLatencyMs = max;
        }
    }

    /// <summary>
    /// Get a formatted string with current latency statistics.
    /// </summary>
    public string GetStatisticsString()
    {
        lock (_lock)
        {
            return $"Latency: {LastLatencyMs:F2}ms (avg: {AverageLatencyMs:F2}ms, min: {MinLatencyMs:F2}ms, max: {MaxLatencyMs:F2}ms)";
        }
    }

    /// <summary>
    /// Log current statistics to debug output.
    /// </summary>
    public void LogStatistics(string context = "")
    {
        string prefix = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
        Debug.WriteLine($"{prefix}{GetStatisticsString()}");
    }

    /// <summary>
    /// Reset all statistics.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _recentLatencies.Clear();
            AverageLatencyMs = 0;
            MinLatencyMs = double.MaxValue;
            MaxLatencyMs = 0;
            LastLatencyMs = 0;
            TotalSamples = 0;
        }
    }
}
