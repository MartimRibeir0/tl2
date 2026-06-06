using Microsoft.AspNetCore.Mvc;
using KubeManager.API.Services;
using KubeManager.API.Models;

namespace KubeManager.API.Controllers;

[ApiController]
[Route("api/k8ssecrets")]
public class K8sSecretsController : ControllerBase
{
    private readonly KubernetesService _svc;
    public K8sSecretsController(KubernetesService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? ns = null)
        => Ok(await _svc.GetSecretsAsync(ns));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSecretDto dto)
    {
        try
        {
            await _svc.CreateTlsSecretAsync(dto.Namespace, dto.Name, dto.Key, dto.Cert);
            return Ok(new { message = "Secret TLS criado com sucesso" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao criar secret: {ex.Message}" });
        }
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, [FromQuery] string ns = "default")
    {
        try
        {
            await _svc.DeleteSecretAsync(ns, name);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao apagar secret: {ex.Message}" });
        }
    }

    [HttpPost("generate-self-signed")]
    public IActionResult GenerateSelfSigned([FromBody] GenerateCertDto dto)
    {
        try
        {
            var (key, cert) = _svc.GenerateSelfSignedCert(dto.CommonName);
            return Ok(new { key, cert });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Erro ao gerar certificado: {ex.Message}" });
        }
    }
}

public record GenerateCertDto(string CommonName);