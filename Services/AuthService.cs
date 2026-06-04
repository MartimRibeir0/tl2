using System.Text.Json;
using KubeManager.API.Models;

namespace KubeManager.API.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponseDto> LoginAsync(string username, string password, string url, string realm = "pam")
    {
        var baseUrl = url.EndsWith("/") ? url : url + "/";
        if (!baseUrl.EndsWith("api2/json/")) baseUrl += "api2/json/";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", username },
            { "password", password },
            { "realm", realm }
        });

        try
        {
            var response = await _httpClient.PostAsync(baseUrl + "access/ticket", content);
            if (!response.IsSuccessStatusCode)
            {
                return new AuthResponseDto("", "", "", false, "Credenciais inválidas no Proxmox.");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            return new AuthResponseDto(
                data.GetProperty("ticket").GetString()!,
                data.GetProperty("CSRFPreventionToken").GetString()!,
                data.GetProperty("username").GetString()!,
                true
            );
        }
        catch (Exception ex)
        {
            return new AuthResponseDto("", "", "", false, $"Erro de rede ({url}): {ex.Message}");
        }
    }
}
