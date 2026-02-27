namespace ChaosWebApp.Models;

public enum ChaosTarget { WebApp, WebApi, Both }
public enum FrequencyType { EveryNRequests, EveryNSeconds, Percentage }

public class ChaosConfig
{
    public bool IsEnabled { get; set; }
    public ChaosTarget Target { get; set; } = ChaosTarget.Both;

    // Issue types
    public bool EnableHighCpu { get; set; }
    public bool EnableHighMemory { get; set; }
    public bool EnableHighLatency { get; set; }
    public bool EnableError404 { get; set; }
    public bool EnableError500 { get; set; }
    public bool EnableError503 { get; set; }
    public bool EnableError429 { get; set; }
    public bool EnableStackOverflow { get; set; }
    public bool EnableSlowResponse { get; set; }
    public bool EnableLongStartup { get; set; }

    // Parameters
    public bool UseInterval { get; set; }
    public int CpuDurationMs { get; set; } = 2000;
    public int MemorySizeMb { get; set; } = 100;
    public int LatencyMs { get; set; } = 3000;
    public int SlowResponseMs { get; set; } = 5000;
    public int StartupDurationMinMs { get; set; } = 5000;
    public int StartupDurationMaxMs { get; set; } = 30000;

    // Interval min/max (used when UseInterval = true)
    public int CpuDurationMinMs { get; set; } = 500;
    public int CpuDurationMaxMs { get; set; } = 5000;
    public int MemorySizeMinMb { get; set; } = 50;
    public int MemorySizeMaxMb { get; set; } = 500;
    public int LatencyMinMs { get; set; } = 500;
    public int LatencyMaxMs { get; set; } = 10000;
    public int SlowResponseMinMs { get; set; } = 500;
    public int SlowResponseMaxMs { get; set; } = 10000;

    // Frequency
    public FrequencyType FrequencyType { get; set; } = FrequencyType.Percentage;
    public int EveryNRequests { get; set; } = 10;
    public int EveryNSeconds { get; set; } = 30;
    public int Percentage { get; set; } = 10;
}
