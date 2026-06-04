using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;
using KubeManager.API.Models;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NamespacesController : ControllerBase
{
    private readonly KubernetesService _svc;
    public NamespacesController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _svc.GetNamespacesAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNamespaceDto dto)
    {
        try 
        {
            await _svc.CreateNamespaceAsync(dto.Name);
            return Ok(new { message = "Namespace criado com sucesso" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao criar namespace: {ex.Message}" });
        }
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        await _svc.DeleteNamespaceAsync(name);
        return Ok();
    }
}