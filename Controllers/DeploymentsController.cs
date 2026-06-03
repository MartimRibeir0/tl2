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
    public async Task<IActionResult> Create(CreateDeploymentDto dto)
    {
        await _svc.CreateDeploymentAsync(dto.Namespace, dto.Name, dto.Image, dto.Replicas, dto.EnvVars, dto.CpuLimit, dto.MemLimit);
        return Ok();
    }


    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, [FromQuery] string ns = "default")
    {
        await _svc.DeleteDeploymentAsync(ns, name);
        return Ok();
    }

    [HttpPatch("scale")]
    public async Task<IActionResult> Scale([FromBody] ScaleDeploymentDto dto)
    {
        await _svc.ScaleDeploymentAsync(dto.Namespace, dto.Name, dto.Replicas);
        return Ok();
    }
}