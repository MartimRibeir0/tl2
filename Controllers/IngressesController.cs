using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;
using KubeManager.API.Models;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngressesController : ControllerBase
{
    private readonly KubernetesService _svc;
    public IngressesController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? ns = null)
        => Ok(await _svc.GetIngressesAsync(ns));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIngressDto dto)
    {
        try
        {
            await _svc.CreateIngressAsync(dto.Namespace, dto.Name, dto.Host, dto.ServiceName, dto.Port, dto.Path, dto.PathType, dto.TlsSecret, dto.Annotations);
            return Ok(new { message = "Ingress criado com sucesso" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao criar ingress: {ex.Message}" });
        }
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, [FromQuery] string ns = "default")
    {
        try
        {
            await _svc.DeleteIngressAsync(ns, name);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao apagar ingress: {ex.Message}" });
        }
    }
}
