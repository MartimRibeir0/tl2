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
}