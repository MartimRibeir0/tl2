using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly KubernetesService _svc;
    public NodesController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _svc.GetNodesAsync());

    [HttpPut("{name}/role")]
    public async Task<IActionResult> UpdateRole(string name, [FromBody] RoleUpdateDto dto)
    {
        try
        {
            await _svc.UpdateNodeRoleAsync(name, dto.Role);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class RoleUpdateDto
    {
        public string Role { get; set; } = string.Empty;
    }
}