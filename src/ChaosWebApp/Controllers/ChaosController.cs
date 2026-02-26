using ChaosWebApp.Models;
using ChaosWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChaosWebApp.Controllers;

/// <summary>
/// Chaos configuration API — query and update chaos settings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChaosController : ControllerBase
{
    private readonly IChaosService _chaosService;

    public ChaosController(IChaosService chaosService)
    {
        _chaosService = chaosService;
    }

    /// <summary>Get the current chaos configuration.</summary>
    /// <response code="200">Current configuration.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ChaosConfig), StatusCodes.Status200OK)]
    public ActionResult<ChaosConfig> GetConfig()
    {
        return Ok(_chaosService.GetConfig());
    }

    /// <summary>Update the chaos configuration.</summary>
    /// <param name="config">New configuration.</param>
    /// <response code="200">Configuration updated.</response>
    [HttpPut]
    [ProducesResponseType(typeof(ChaosConfig), StatusCodes.Status200OK)]
    public ActionResult<ChaosConfig> UpdateConfig([FromBody] ChaosConfig config)
    {
        _chaosService.UpdateConfig(config);
        return Ok(_chaosService.GetConfig());
    }

    /// <summary>Get currently active chaos types.</summary>
    /// <response code="200">List of active chaos types.</response>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetActiveChaosTypes()
    {
        return Ok(new
        {
            isEnabled = _chaosService.GetConfig().IsEnabled,
            activeTypes = _chaosService.GetActiveChaosTypes()
        });
    }
}
