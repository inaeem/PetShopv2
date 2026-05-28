using Microsoft.Extensions.Logging;
using PetShop.Data.Diagnostics;
using PetShop.Data.UnitOfWork;
using PetShop.Domain.Entities;
using PetShop.Service.Common;
using PetShop.Service.DTOs;
using PetShop.Service.Security;

namespace PetShop.Service.Services;

public class AuthService : IAuthService
{
    private const string Category = "PetShop.Service.AuthService";

    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;
    private readonly ILayerTracer _tracer;

    public AuthService(IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokens,
        ILogger<AuthService> logger, ILayerTracer tracer)
    {
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
        _logger = logger;
        _tracer = tracer;
    }

    private Task<T> Measure<T>(string member, object? args, Func<Task<T>> body)
        => _tracer.MeasureAsync(LayerKind.Service, Category, member, body, args);

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        // Note: never trace the password — only the username.
        => Measure(nameof(LoginAsync), new { request.Username }, async () =>
        {
            var user = await _uow.Users.GetByUsernameAsync(request.Username, ct);
            if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login for {Username}", request.Username);
                throw AppException.Unauthorized();
            }

            return BuildResponse(user);
        });

    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        // Note: never trace the password — only the non-secret fields.
        => Measure(nameof(RegisterAsync), new { request.Username, request.Email }, async () =>
        {
            if (await _uow.Users.GetByUsernameAsync(request.Username, ct) is not null)
                throw AppException.Conflict("Username is already taken.");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _hasher.Hash(request.Password),
                Roles = "User",
                IsActive = true
            };

            await _uow.Users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Registered user {Username}", user.Username);

            return BuildResponse(user);
        });

    private AuthResponse BuildResponse(User user)
    {
        var (token, expiresUtc) = _tokens.CreateAccessToken(user);
        var roles = user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new AuthResponse(token, expiresUtc, user.Username, roles);
    }
}
