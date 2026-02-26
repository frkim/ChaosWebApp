using ChaosWebApp.Models;

namespace ChaosWebApp.Services;

public interface IChaosService
{
    ChaosConfig GetConfig();
    void UpdateConfig(ChaosConfig config);
    bool ShouldTriggerChaos(string context);
    IReadOnlyList<string> GetActiveChaosTypes();
}

public class ChaosService : IChaosService
{
    // _config is replaced atomically via a reference swap — the object itself is never
    // mutated after assignment, so volatile is sufficient for single-field atomicity.
    private volatile ChaosConfig _config = new();
    private int _requestCounter = 0;
    private DateTime _lastChaosTime = DateTime.MinValue;

    public ChaosConfig GetConfig() => _config;

    public void UpdateConfig(ChaosConfig config)
    {
        _config = config;
    }

    public bool ShouldTriggerChaos(string context)
    {
        var cfg = _config;
        if (!cfg.IsEnabled) return false;

        // Check target
        var isApi = context == "api";
        var isApp = context == "app";
        if (cfg.Target == ChaosTarget.WebApi && !isApi) return false;
        if (cfg.Target == ChaosTarget.WebApp && !isApp) return false;

        // Check frequency
        switch (cfg.FrequencyType)
        {
            case FrequencyType.EveryNRequests:
                var counter = Interlocked.Increment(ref _requestCounter);
                return counter % cfg.EveryNRequests == 0;

            case FrequencyType.EveryNSeconds:
                lock (this)
                {
                    if ((DateTime.UtcNow - _lastChaosTime).TotalSeconds >= cfg.EveryNSeconds)
                    {
                        _lastChaosTime = DateTime.UtcNow;
                        return true;
                    }
                    return false;
                }

            case FrequencyType.Percentage:
            default:
                return Random.Shared.Next(100) < cfg.Percentage;
        }
    }

    public IReadOnlyList<string> GetActiveChaosTypes()
    {
        var cfg = _config;
        var types = new List<string>();
        if (!cfg.IsEnabled) return types;

        if (cfg.EnableHighCpu) types.Add("HighCpu");
        if (cfg.EnableHighMemory) types.Add("HighMemory");
        if (cfg.EnableHighLatency) types.Add("HighLatency");
        if (cfg.EnableError404) types.Add("Error404");
        if (cfg.EnableError500) types.Add("Error500");
        if (cfg.EnableError503) types.Add("Error503");
        if (cfg.EnableError429) types.Add("Error429");
        if (cfg.EnableStackOverflow) types.Add("StackOverflow");
        if (cfg.EnableSlowResponse) types.Add("SlowResponse");

        return types;
    }
}
