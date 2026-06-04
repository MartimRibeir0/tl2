using Microsoft.AspNetCore.Mvc;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly string _clustersPath;
    private readonly HttpClient _http;

    public ConfigController()
    {
        _clustersPath = Path.Combine(AppContext.BaseDirectory, "clusters");
        if (!Directory.Exists(_clustersPath)) Directory.CreateDirectory(_clustersPath);
        _http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });
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
        var files = Directory.GetFiles(_clustersPath, "*.yaml");
        return Ok(files.Select(Path.GetFileNameWithoutExtension));
    }

    [HttpPost("clusters")]
    public async Task<IActionResult> SaveCluster([FromBody] SaveClusterDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name) || string.IsNullOrEmpty(dto.Yaml))
            return BadRequest("Nome e YAML são obrigatórios.");

        var fileName = $"{dto.Name.ToLower().Replace(" ", "-")}.yaml";
        var filePath = Path.Combine(_clustersPath, fileName);
        
        await System.IO.File.WriteAllTextAsync(filePath, dto.Yaml);
        return Ok(new { message = "Configuração guardada com sucesso!" });
    }

    [HttpDelete("clusters/{name}")]
    public IActionResult DeleteCluster(string name)
    {
        var filePath = Path.Combine(_clustersPath, $"{name}.yaml");
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            return Ok();
        }
        return NotFound();
    }
}

public record SaveClusterDto(string Name, string Yaml);
