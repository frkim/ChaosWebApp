using ChaosWebApp.Models;
using ChaosWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChaosWebApp.Pages;

[IgnoreAntiforgeryToken]
public class ChaosConfigModel : PageModel
{
    private readonly IChaosService _chaosService;

    public ChaosConfigModel(IChaosService chaosService)
    {
        _chaosService = chaosService;
    }

    public ChaosConfig Config { get; private set; } = new();

    public void OnGet()
    {
        Config = _chaosService.GetConfig();
    }

    public IActionResult OnPostSave(
        bool IsEnabled,
        int Target,
        bool UseInterval,
        bool EnableHighCpu,
        bool EnableHighMemory,
        bool EnableHighLatency,
        bool EnableError404,
        bool EnableError500,
        bool EnableError503,
        bool EnableError429,
        bool EnableStackOverflow,
        bool EnableSlowResponse,
        bool EnableLongStartup,
        int CpuDurationMs,
        int MemorySizeMb,
        int LatencyMs,
        int SlowResponseMs,
        int CpuDurationMinMs,
        int CpuDurationMaxMs,
        int MemorySizeMinMb,
        int MemorySizeMaxMb,
        int LatencyMinMs,
        int LatencyMaxMs,
        int SlowResponseMinMs,
        int SlowResponseMaxMs,
        int StartupDurationMinMs,
        int StartupDurationMaxMs,
        int FrequencyType,
        int EveryNRequests,
        int EveryNSeconds,
        int Percentage)
    {
        var config = new ChaosConfig
        {
            IsEnabled = IsEnabled,
            Target = (ChaosTarget)Target,
            UseInterval = UseInterval,
            EnableHighCpu = EnableHighCpu,
            EnableHighMemory = EnableHighMemory,
            EnableHighLatency = EnableHighLatency,
            EnableError404 = EnableError404,
            EnableError500 = EnableError500,
            EnableError503 = EnableError503,
            EnableError429 = EnableError429,
            EnableStackOverflow = EnableStackOverflow,
            EnableSlowResponse = EnableSlowResponse,
            EnableLongStartup = EnableLongStartup,
            CpuDurationMs = Math.Clamp(CpuDurationMs, 100, 30000),
            MemorySizeMb = Math.Clamp(MemorySizeMb, 10, 2000),
            LatencyMs = Math.Clamp(LatencyMs, 100, 60000),
            SlowResponseMs = Math.Clamp(SlowResponseMs, 100, 60000),
            CpuDurationMinMs = Math.Clamp(CpuDurationMinMs, 100, 30000),
            CpuDurationMaxMs = Math.Clamp(CpuDurationMaxMs, 100, 30000),
            MemorySizeMinMb = Math.Clamp(MemorySizeMinMb, 10, 2000),
            MemorySizeMaxMb = Math.Clamp(MemorySizeMaxMb, 10, 2000),
            LatencyMinMs = Math.Clamp(LatencyMinMs, 100, 60000),
            LatencyMaxMs = Math.Clamp(LatencyMaxMs, 100, 60000),
            SlowResponseMinMs = Math.Clamp(SlowResponseMinMs, 100, 60000),
            SlowResponseMaxMs = Math.Clamp(SlowResponseMaxMs, 100, 60000),
            StartupDurationMinMs = Math.Clamp(StartupDurationMinMs, 1000, 120000),
            StartupDurationMaxMs = Math.Clamp(StartupDurationMaxMs, 1000, 300000),
            FrequencyType = (Models.FrequencyType)FrequencyType,
            EveryNRequests = Math.Max(1, EveryNRequests),
            EveryNSeconds = Math.Max(1, EveryNSeconds),
            Percentage = Math.Clamp(Percentage, 1, 100)
        };

        _chaosService.UpdateConfig(config);

        TempData["Success"] = IsEnabled
            ? "Configuration saved — chaos is now ACTIVE."
            : "Configuration saved — chaos is disabled.";

        return RedirectToPage();
    }
}
