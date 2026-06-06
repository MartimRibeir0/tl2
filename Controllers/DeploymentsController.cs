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
    public async Task<IActionResult> Get([FromQuery] string? ns = null)
        => Ok(await _svc.GetDeploymentsAsync(ns));

    [HttpPost]
    public async Task<IActionResult> Create(CreateDeploymentDto dto)
    {
        try
        {
            await _svc.CreateDeploymentAsync(dto.Namespace, dto.Name, dto.Image, dto.Replicas, dto.ContainerPort, dto.EnvVars, dto.CpuLimit, dto.MemLimit, dto.ImagePullPolicy, dto.UpdateStrategy);
            return Ok(new { message = "Deployment criado com sucesso" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao criar deployment: {ex.Message}" });
        }
    }


    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, [FromQuery] string ns = "default")
    {
        try
        {
            await _svc.DeleteDeploymentAsync(ns, name);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao apagar deployment: {ex.Message}" });
        }
    }

    [HttpPatch("scale")]
    public async Task<IActionResult> Scale([FromBody] ScaleDeploymentDto dto)
    {
        try
        {
            await _svc.ScaleDeploymentAsync(dto.Namespace, dto.Name, dto.Replicas);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao escalar deployment: {ex.Message}" });
        }
    }
}