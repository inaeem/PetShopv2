namespace PetShop.Service.DTOs;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Email, string Password);

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresUtc,
    string Username,
    IReadOnlyList<string> Roles);
