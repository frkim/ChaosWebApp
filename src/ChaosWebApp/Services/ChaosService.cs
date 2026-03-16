using System.Text.Json;
using Azure.Data.AppConfiguration;
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
    private const string AppConfigKey = "ChaosWebApp:ChaosConfig";
    private static readonly string LocalConfigPath = Path.Combine(
        AppContext.BaseDirectory, "chaos-config.json");

    private volatile ChaosConfig _config = new();
    private int _requestCounter = 0;
    private DateTime _lastChaosTime = DateTime.MinValue;
    private readonly ConfigurationClient? _configClient;
    private readonly ILogger<ChaosService> _logger;
    private readonly object _fileLock = new();

    public ChaosService(ILogger<ChaosService> logger, ConfigurationClient? configClient = null)
    {
        _logger = logger;
        _configClient = configClient;
        LoadConfig();
    }

    public ChaosConfig GetConfig() => _config;

    public void UpdateConfig(ChaosConfig config)
    {
        _config = config;
        SaveConfig(config);
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

    private void LoadConfig()
    {
        if (_configClient is not null)
        {
            try
            {
                var response = _configClient.GetConfigurationSetting(AppConfigKey);
                if (response?.Value?.Value is not null)
                {
                    var loaded = JsonSerializer.Deserialize<ChaosConfig>(response.Value.Value);
                    if (loaded is not null)
                    {
                        _config = loaded;
                        _logger.LogInformation("Chaos config loaded from Azure App Configuration.");
                        return;
                    }
                }
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("No chaos config found in App Configuration; using defaults.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load chaos config from App Configuration; using defaults.");
            }
        }

        LoadFromLocalFile();
    }

    private void SaveConfig(ChaosConfig config)
    {
        if (_configClient is not null)
        {
            try
            {
                var json = JsonSerializer.Serialize(config);
                _configClient.SetConfigurationSetting(AppConfigKey, json);
                _logger.LogInformation("Chaos config saved to Azure App Configuration.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save chaos config to App Configuration; falling back to local file.");
            }
        }

        SaveToLocalFile(config);
    }

    private void LoadFromLocalFile()
    {
        try
        {
            if (!File.Exists(LocalConfigPath)) return;

            string json;
            lock (_fileLock)
            {
                json = File.ReadAllText(LocalConfigPath);
            }
            var loaded = JsonSerializer.Deserialize<ChaosConfig>(json);
            if (loaded is not null)
            {
                _config = loaded;
                _logger.LogInformation("Chaos config loaded from local file {Path}.", LocalConfigPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load chaos config from local file; using defaults.");
        }
    }

    private void SaveToLocalFile(ChaosConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            lock (_fileLock)
            {
                File.WriteAllText(LocalConfigPath, json);
            }
            _logger.LogInformation("Chaos config saved to local file {Path}.", LocalConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save chaos config to local file; config remains in-memory only.");
        }
    }
}
