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
    public async Task<IActionResult> Get([FromQuery] string ns = "default")
        => Ok(await _svc.GetIngressesAsync(ns));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIngressDto dto)
    {
        await _svc.CreateIngressAsync(dto.Namespace, dto.Name, dto.Host, dto.ServiceName, dto.Port);
        return Ok();
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, [FromQuery] string ns = "default")
    {
        await _svc.DeleteIngressAsync(ns, name);
        return Ok();
    }
}
