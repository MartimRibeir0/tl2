using System.Net.Http.Headers;
using System.Text.Json;
using KubeManager.API.Models;

namespace KubeManager.API.Services;

public class ProxmoxService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public ProxmoxService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;

        var baseUrl = _config["Proxmox:BaseUrl"];
        var tokenId = _config["Proxmox:TokenId"];
        var secret = _config["Proxmox:Secret"];

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(tokenId) || string.IsNullOrEmpty(secret))
        {
            throw new Exception("Proxmox configuration is missing in appsettings.json");
        }

        _httpClient.BaseAddress = new Uri(baseUrl);
        
        // Formato do Token: PVEAPIToken=USER@REALM!TOKENID=UUID
        // Usamos TryAddWithoutValidation porque o formato do Proxmox não segue o padrão "Scheme Parameter" do .NET
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"PVEAPIToken={tokenId}={secret}");
        
        // Ignorar erros de SSL se o Proxmox usar certificado self-signed (comum em labs)
        // Nota: Em produção, isto deve ser tratado corretamente.
    }

    public async Task<IEnumerable<VirtualMachineDto>> GetVmsAsync()
    {
        var response = await _httpClient.GetAsync("cluster/resources?type=vm");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        return data.EnumerateArray().Select(vm => new VirtualMachineDto(
            vm.GetProperty("vmid").GetInt32(),
            vm.GetProperty("name").GetString() ?? "Unknown",
            vm.GetProperty("status").GetString() ?? "Unknown",
            vm.GetProperty("node").GetString() ?? "Unknown",
            vm.TryGetProperty("cpu", out var cpu) ? cpu.GetDouble() : 0,
            vm.TryGetProperty("mem", out var mem) ? mem.GetInt64() : 0,
            vm.TryGetProperty("uptime", out var uptime) ? uptime.GetInt64() : 0
        )).ToList(); // Materializar os dados antes de fechar o JsonDocument
    }

    public async Task<bool> PerformVmActionAsync(string node, int vmid, string action)
    {
        // Actions: start, stop, shutdown, reboot, pause, resume
        var response = await _httpClient.PostAsync($"nodes/{node}/qemu/{vmid}/status/{action}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CloneVmAsync(string node, int vmid, CloneVmDto dto)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "newid", dto.NewId.ToString() },
            { "name", dto.Name },
            { "target", dto.TargetNode },
            { "full", dto.Full ? "1" : "0" }
        });

        var response = await _httpClient.PostAsync($"nodes/{node}/qemu/{vmid}/clone", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteVmAsync(string node, int vmid)
    {
        var response = await _httpClient.DeleteAsync($"nodes/{node}/qemu/{vmid}");
        return response.IsSuccessStatusCode;
    }
}
