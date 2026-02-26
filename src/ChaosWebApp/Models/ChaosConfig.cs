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
    public bool EnableRandomErrors { get; set; }

    // Parameters
    public int CpuDurationMs { get; set; } = 2000;
    public int MemorySizeMb { get; set; } = 100;
    public int LatencyMs { get; set; } = 3000;
    public int SlowResponseMs { get; set; } = 5000;

    // Frequency
    public FrequencyType FrequencyType { get; set; } = FrequencyType.Percentage;
    public int EveryNRequests { get; set; } = 10;
    public int EveryNSeconds { get; set; } = 30;
    public int Percentage { get; set; } = 10;
}
