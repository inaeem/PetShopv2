using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PetShop.Api.Common;
using PetShop.Api.Data;
using PetShop.Api.Filters;
using PetShop.Api.Middleware;
using PetShop.Data;
using PetShop.Service;
using PetShop.Service.Common;
using PetShop.Service.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging (Serilog) — reads the "Serilog" section from appsettings.json.
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// ---------------------------------------------------------------------------
// Layers
// ---------------------------------------------------------------------------
builder.Services.AddDataLayer(builder.Configuration);
builder.Services.AddServiceLayer(builder.Configuration);

// ---------------------------------------------------------------------------
// Controllers + global filters
// ---------------------------------------------------------------------------
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequestLoggingFilter>();
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<ApiExceptionFilter>();
});

// Make [ApiController] model-binding failures (malformed JSON, wrong types,
// missing required fields) return the same ApiResponse envelope as FluentValidation.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kv => kv.Value is { Errors.Count: > 0 })
            .SelectMany(kv => kv.Value!.Errors.Select(e =>
                string.IsNullOrWhiteSpace(e.ErrorMessage) ? "The request payload could not be parsed." : e.ErrorMessage))
            .ToList();

        return new BadRequestObjectResult(
            ApiResponse<object>.Fail("Invalid request payload.", errors));
    };
});

// ---------------------------------------------------------------------------
// JWT authentication + validation
// ---------------------------------------------------------------------------
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Jwt settings are not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Return our ApiResponse envelope instead of an empty body for auth failures.
        options.Events = new JwtBearerEvents
        {
            // Fires when a token was supplied but failed validation (bad signature,
            // wrong issuer/audience, expired). Flags expiry so the client can refresh.
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                    context.Response.Headers["Token-Expired"] = "true";
                return Task.CompletedTask;
            },

            // 401 — no token, malformed token, or a token that failed validation above.
            OnChallenge = context =>
            {
                context.HandleResponse(); // suppress the default empty 401
                var expired = context.AuthenticateFailure is SecurityTokenExpiredException;
                var message = expired
                    ? "Your session has expired. Please sign in again."
                    : "Authentication is required and the supplied token is missing or invalid.";
                return ProblemResponseWriter.WriteAsync(
                    context.HttpContext, StatusCodes.Status401Unauthorized, message);
            },

            // 403 — authenticated, but lacking the required role/policy.
            OnForbidden = context => ProblemResponseWriter.WriteAsync(
                context.HttpContext, StatusCodes.Status403Forbidden,
                "You do not have permission to perform this action.")
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// Swagger / OpenAPI with JWT bearer support
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pet Shop API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT token returned by /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

builder.Services.AddCors(options => options.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
// Outermost: stamp every downstream log line with a shared correlation id.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// One enriched summary line per request: method, path, status, elapsed, plus
// the authenticated user, trace id (to correlate with other log lines for the
// same request) and basic client info.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("User", httpContext.User.Identity?.Name ?? "anonymous");
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

// Swagger is exposed in Development and QA only — never in Production.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("QA"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pet Shop API v1");
        c.RoutePrefix = "swagger";
    });
}

// Static web (messaging page) served from wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations / seed admin user.
await DbInitializer.InitializeAsync(app.Services, app.Configuration);

app.Run();

/// <summary>
/// Exposes the implicit top-level Program class so the e2e test project can use
/// <c>WebApplicationFactory&lt;Program&gt;</c> to spin up the API in-process.
/// </summary>
public partial class Program { }
