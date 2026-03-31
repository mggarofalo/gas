using FluentValidation;
using GasTracker.Api.Endpoints;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using GasTracker.Infrastructure.Repositories;
using GasTracker.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IFillUpRepository, FillUpRepository>();

builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("MinIO"));
builder.Services.AddSingleton<IReceiptStore, MinioReceiptStore>();

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
    db.Database.Migrate();
}

// Ensure MinIO bucket exists
var receiptStore = app.Services.GetRequiredService<IReceiptStore>();
await receiptStore.EnsureBucketExistsAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapVehicleEndpoints();
app.MapFillUpEndpoints();
app.MapStatsEndpoints();
app.MapLocationEndpoints();

app.Run();
