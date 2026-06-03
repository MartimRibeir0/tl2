using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class YamlController : ControllerBase
{
    private readonly KubernetesService _svc;
    public YamlController(KubernetesService svc) => _svc = svc;

    [HttpGet("{resource}/{name}")]
    public async Task<IActionResult> Get(string resource, string name, [FromQuery] string ns = "default")
    {
        try {
            var yaml = await _svc.GetYamlAsync(resource, ns, name);
            return Ok(new { yaml });
        } catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{resource}/{name}")]
    public async Task<IActionResult> Update(string resource, string name, [FromBody] YamlUpdateDto dto, [FromQuery] string ns = "default")
    {
        try {
            await _svc.UpdateYamlAsync(resource, ns, name, dto.Yaml);
            return Ok();
        } catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}

public record YamlUpdateDto(string Yaml);
