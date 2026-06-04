using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly KubernetesService _svc;
    public EventsController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? ns = null)
        => Ok(await _svc.GetEventsAsync(ns));
}
