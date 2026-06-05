using System.Net.Http.Headers;
using System.Text.Json;
using KubeManager.API.Models;

namespace KubeManager.API.Services;

public class ProxmoxService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProxmoxService(HttpClient httpClient, IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _config = config;
        _httpContextAccessor = httpContextAccessor;
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null, Dictionary<string, string>? query = null)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) throw new Exception("Contexto HTTP não encontrado");

        var ticket = context.Request.Headers["X-PVE-Ticket"].ToString();
        var csrf = context.Request.Headers["X-PVE-CSRF"].ToString();
        var pveUrl = context.Request.Headers["X-PVE-Url"].ToString();

        if (string.IsNullOrEmpty(pveUrl)) throw new Exception("URL do Proxmox não fornecida");

        var baseUrl = pveUrl.EndsWith("/") ? pveUrl : pveUrl + "/";
        if (!baseUrl.Contains("api2/json/")) baseUrl += "api2/json/";

        var fullUrl = baseUrl + path;
        if (query != null)
        {
            var q = string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            fullUrl += (fullUrl.Contains("?") ? "&" : "?") + q;
        }

        var request = new HttpRequestMessage(method, fullUrl);
        
        if (!string.IsNullOrEmpty(ticket))
            request.Headers.Add("Cookie", $"PVEAuthCookie={ticket}");

        if (!string.IsNullOrEmpty(csrf))
            request.Headers.Add("CSRFPreventionToken", csrf);

        if (body != null)
        {
            if (body is HttpContent content) request.Content = content;
            else request.Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
        }

        return await _httpClient.SendAsync(request);
    }

    public async Task<IEnumerable<VirtualMachineDto>> GetVmsAsync()
    {
        var response = await SendAsync(HttpMethod.Get, "cluster/resources", query: new Dictionary<string, string> { { "type", "vm" } });
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
        )).ToList();
    }

    public async Task<bool> PerformVmActionAsync(string node, int vmid, string action)
    {
        var response = await SendAsync(HttpMethod.Post, $"nodes/{node}/qemu/{vmid}/status/{action}");
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

        var response = await SendAsync(HttpMethod.Post, $"nodes/{node}/qemu/{vmid}/clone", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteVmAsync(string node, int vmid)
    {
        var response = await SendAsync(HttpMethod.Delete, $"nodes/{node}/qemu/{vmid}");
        return response.IsSuccessStatusCode;
    }
}
