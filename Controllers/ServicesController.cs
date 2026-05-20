using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;
using KubeManager.API.Models;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly KubernetesService _svc;
    public ServicesController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string ns = "default")
        => Ok(await _svc.GetServicesAsync(ns));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceDto dto)
    {
        await _svc.CreateServiceAsync(dto.Namespace, dto.Name, dto.AppLabel, dto.Port, dto.TargetPort);
        return Ok();
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, [FromQuery] string ns = "default")
    {
        await _svc.DeleteServiceAsync(ns, name);
        return Ok();
    }
}