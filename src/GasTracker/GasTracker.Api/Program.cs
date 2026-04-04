using System.Text;
using FluentValidation;
using GasTracker.Api.Endpoints;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Auth;
using GasTracker.Infrastructure.Data;
using GasTracker.Infrastructure.Repositories;
using GasTracker.Infrastructure.Storage;
using GasTracker.Infrastructure.Paperless;
using GasTracker.Infrastructure.Ynab;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Data Protection (encrypts YNAB API tokens at rest)
var dpBuilder = builder.Services.AddDataProtection();
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/dp-keys";
var dpKeysDir = new DirectoryInfo(dpKeysPath);
dpKeysDir.Create();
dpBuilder.PersistKeysToFileSystem(dpKeysDir);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

// Identity
builder.Services.AddIdentityCore<ApplicationUser>(opts =>
    {
        opts.Password.RequiredLength = 12;
        opts.Password.RequireDigit = true;
        opts.Password.RequireUppercase = true;
        opts.Password.RequireNonAlphanumeric = true;
        opts.Password.RequireLowercase = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required");
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "gas-api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "gas-app",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });
builder.Services.AddAuthorization();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Repositories
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IFillUpRepository, FillUpRepository>();

// MinIO
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("MinIO"));
builder.Services.AddSingleton<IReceiptStore, MinioReceiptStore>();

// YNAB API client
builder.Services.AddHttpClient<IYnabClient, YnabClient>(c =>
{
    c.BaseAddress = new Uri("https://api.ynab.com");
    c.Timeout = TimeSpan.FromSeconds(60);
});

// Paperless-ngx
var paperlessOpts = builder.Configuration.GetSection("Paperless").Get<PaperlessOptions>() ?? new PaperlessOptions();
if (paperlessOpts.Enabled && !string.IsNullOrWhiteSpace(paperlessOpts.Token))
{
    builder.Services.AddHttpClient<IPaperlessClient, PaperlessClient>(c =>
    {
        c.BaseAddress = new Uri(paperlessOpts.BaseUrl.TrimEnd('/') + "/");
        c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", paperlessOpts.Token);
        c.Timeout = TimeSpan.FromSeconds(30);
    });
}
else
{
    builder.Services.AddSingleton<IPaperlessClient, NoOpPaperlessClient>();
}
builder.Services.Configure<PaperlessOptions>(builder.Configuration.GetSection("Paperless"));
builder.Services.AddHostedService<PaperlessSyncService>();

// YNAB services
builder.Services.AddScoped<YnabTokenService>();
builder.Services.AddScoped<YnabPullSyncService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgresql")
    .AddCheck<MinioHealthCheck>("minio");

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetValue<string>("AllowedOrigins") ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();

    await AdminSeeder.SeedAsync(scope.ServiceProvider);
}

// Ensure MinIO bucket exists
var receiptStore = app.Services.GetRequiredService<IReceiptStore>();
await receiptStore.EnsureBucketExistsAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Serve React SPA from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Public endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString(),
            }),
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    },
});
app.MapAuthEndpoints();

// Protected endpoints
app.MapVehicleEndpoints();
app.MapFillUpEndpoints();
app.MapStatsEndpoints();
app.MapLocationEndpoints();
app.MapYnabSettingsEndpoints();
app.MapYnabProxyEndpoints();
app.MapYnabBackfillEndpoints();
app.MapYnabImportEndpoints();

// SPA fallback — serve index.html for any non-API, non-file route
app.MapFallbackToFile("index.html");

app.Run();

// Make Program accessible from test projects (WebApplicationFactory<Program>)
public partial class Program { }
