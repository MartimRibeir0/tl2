using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;
using KubeManager.API.Models;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentsController : ControllerBase
{
    private readonly KubernetesService _svc;
    public DeploymentsController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string ns = "default")
        => Ok(await _svc.GetDeploymentsAsync(ns));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeploymentDto dto)
    {
        await _svc.CreateDeploymentAsync(dto.Namespace, dto.Name, dto.Image, dto.Replicas);
        return Ok();
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, [FromQuery] string ns = "default")
    {
        await _svc.DeleteDeploymentAsync(ns, name);
        return Ok();
    }
}