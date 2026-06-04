namespace KubeManager.API.Models;

public record LoginDto(string Username, string Password, string Url, string Realm = "pam");

public record AuthResponseDto(
    string Ticket, 
    string CSRFPreventionToken, 
    string Username, 
    bool Success, 
    string? Message = null
);
