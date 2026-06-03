using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;
using KubeManager.API.Models;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PodsController : ControllerBase
{
    private readonly KubernetesService _svc;
    public PodsController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string ns = "default")
        => Ok(await _svc.GetPodsAsync(ns));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePodDto dto)
    {
        await _svc.CreatePodAsync(dto.Namespace, dto.Name, dto.Image);
        return Ok();
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, [FromQuery] string ns = "default")
    {
        await _svc.DeletePodAsync(ns, name);
        return Ok();
    }

    [HttpGet("{name}/logs")]
    public async Task<IActionResult> GetLogs(string name, [FromQuery] string ns = "default")
        => Ok(new { logs = await _svc.GetPodLogsAsync(ns, name) });
}