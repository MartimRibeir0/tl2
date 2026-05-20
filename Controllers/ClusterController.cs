using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClusterController : ControllerBase
{
    private readonly KubernetesService _svc;
    public ClusterController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _svc.GetClusterInfoAsync());
}   