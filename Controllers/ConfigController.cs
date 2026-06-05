using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly KubernetesService _k8sSvc;
    private static readonly HttpClient _http = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

    public ConfigController(KubernetesService k8sSvc)
    {
        _k8sSvc = k8sSvc;
    }

    [HttpGet("fetch-yaml")]
    public async Task<IActionResult> FetchYaml([FromQuery] string ip, [FromQuery] int port)
    {
        if (string.IsNullOrEmpty(ip)) return BadRequest("IP é obrigatório.");
        
        try
        {
            var url = $"http://{ip}:{port}";
            var response = await _http.GetStringAsync(url);
            return Ok(new { yaml = response });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Erro ao obter YAML: {ex.Message}" });
        }
    }

    [HttpGet("clusters")]
    public IActionResult GetClusters()
    {
        var files = Directory.GetFiles(_k8sSvc.ClustersPath, "*.yaml");
        return Ok(files.Select(Path.GetFileNameWithoutExtension));
    }

    [HttpPost("clusters")]
    public async Task<IActionResult> SaveCluster([FromBody] SaveClusterDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name) || string.IsNullOrEmpty(dto.Yaml))
            return BadRequest("Nome e YAML são obrigatórios.");

        var clusterName = dto.Name.ToLower().Replace(" ", "-");
        var fileName = $"{clusterName}.yaml";
        var filePath = Path.Combine(_k8sSvc.ClustersPath, fileName);
        
        await System.IO.File.WriteAllTextAsync(filePath, dto.Yaml);
        
        // Invalidar cache se o cluster já estiver carregado
        KubernetesService.InvalidateCache(clusterName);
        
        return Ok(new { message = "Configuração guardada com sucesso!" });
    }

    [HttpDelete("clusters/{name}")]
    public IActionResult DeleteCluster(string name)
    {
        var clusterName = name.ToLower().Replace(" ", "-");
        var filePath = Path.Combine(_k8sSvc.ClustersPath, $"{clusterName}.yaml");
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            KubernetesService.InvalidateCache(clusterName);
            return Ok();
        }
        return NotFound();
    }
}

public record SaveClusterDto(string Name, string Yaml);
