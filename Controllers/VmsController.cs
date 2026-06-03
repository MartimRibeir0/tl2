using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;
using KubeManager.API.Models;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VmsController : ControllerBase
{
    private readonly ProxmoxService _proxmoxService;

    public VmsController(ProxmoxService proxmoxService)
    {
        _proxmoxService = proxmoxService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVms()
    {
        try
        {
            var vms = await _proxmoxService.GetVmsAsync();
            return Ok(vms);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{node}/{vmid}/action")]
    public async Task<IActionResult> VmAction(string node, int vmid, [FromBody] VmActionDto dto)
    {
        var success = await _proxmoxService.PerformVmActionAsync(node, vmid, dto.Action);
        if (success) return Ok();
        return BadRequest(new { message = $"Failed to perform {dto.Action} on VM {vmid}" });
    }

    [HttpPost("{node}/{vmid}/clone")]
    public async Task<IActionResult> CloneVm(string node, int vmid, [FromBody] CloneVmDto dto)
    {
        var success = await _proxmoxService.CloneVmAsync(node, vmid, dto);
        if (success) return Ok();
        return BadRequest(new { message = $"Failed to clone VM {vmid}" });
    }

    [HttpDelete("{node}/{vmid}")]
    public async Task<IActionResult> DeleteVm(string node, int vmid)
    {
        var success = await _proxmoxService.DeleteVmAsync(node, vmid);
        if (success) return Ok();
        return BadRequest(new { message = $"Failed to delete VM {vmid}" });
    }
}
