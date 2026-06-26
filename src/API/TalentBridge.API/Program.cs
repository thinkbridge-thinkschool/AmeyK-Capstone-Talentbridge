using Asp.Versioning;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;
using TalentBridge.API.Infrastructure;
using TalentBridge.API.Telemetry;
using TalentBridge.API.Resilience;
using TalentBridge.Applications.Application;
using TalentBridge.Applications.Infrastructure;
using TalentBridge.Applications.Infrastructure.Persistence;
using TalentBridge.Companies.Application.Commands.CreateCompany;
using TalentBridge.Companies.Infrastructure;
using TalentBridge.Companies.Infrastructure.Persistence;
using TalentBridge.Notifications.Application.Queries.GetNotifications;
using TalentBridge.Identity.Infrastructure;
using TalentBridge.Identity.Infrastructure.Persistence;
using TalentBridge.Jobs.Application;
using TalentBridge.Jobs.Infrastructure;
using TalentBridge.Jobs.Infrastructure.Persistence;
using TalentBridge.Notifications.Infrastructure;
using TalentBridge.Notifications.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ── Kestrel: remove Server header; cap body size at 10 MB ────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

// ── Module registrations ──────────────────────────────────────────────────────
// EF Core connection strings in each module use "Authentication=Active Directory Default"
// — no password. DefaultAzureCredential picks up az login locally, Managed Identity in Azure.
builder.Services.AddIdentityModule(config);
builder.Services.AddJobsInfrastructure(config);
builder.Services.AddJobsApplication();
builder.Services.AddApplicationsInfrastructure(config);
builder.Services.AddApplicationsApplication();
builder.Services.AddCompaniesModule(config);
builder.Services.AddNotificationsModule(config);

// ── Azure clients — Managed Identity (no connection string keys) ──────────────
var credential = ManagedCredential.Create();
var sbNamespace = config["ServiceBus:Namespace"]
    ?? "talentbridge-sb-amey.servicebus.windows.net";
builder.Services.AddSingleton(new ServiceBusClient(sbNamespace, credential));
var storageUri = config["Storage:ServiceUri"]
    ?? "https://talentbridgestamey2.blob.core.windows.net/";
builder.Services.AddSingleton(new BlobServiceClient(new Uri(storageUri), credential));

var storageConnection = config["Storage:ConnectionString"];
if (!string.IsNullOrWhiteSpace(storageConnection) && storageConnection != "SET_IN_KEYVAULT")
    builder.Services.AddSingleton(new BlobServiceClient(storageConnection));
else
    builder.Services.AddSingleton(new BlobServiceClient(new Uri("https://placeholder.blob.core.windows.net")));
// ── OpenTelemetry → Azure App Insights ───────────────────────────────────────
var appInsightsCs = config["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? config["AzureMonitor:ConnectionString"];

var otelBuilder = builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService("TalentBridge", serviceVersion: "1.0.0"))
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.RecordException = true;
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddSqlClientInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true;
            opts.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddSource(TalentBridgeDiagnostics.SourceName));

if (!string.IsNullOrWhiteSpace(appInsightsCs))
    otelBuilder.UseAzureMonitor();

// ── Caching ───────────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHybridCache();

// ── MediatR (scan all module assemblies) ─────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(TalentBridge.Identity.Application.Commands.Login.LoginCommand).Assembly,
        typeof(TalentBridge.Jobs.Application.Commands.PostJob.PostJobCommand).Assembly,
        typeof(TalentBridge.Applications.Application.Commands.Apply.ApplyCommand).Assembly,
        typeof(TalentBridge.Companies.Application.Commands.CreateCompany.CreateCompanyCommand).Assembly,
        typeof(TalentBridge.Notifications.Application.Queries.GetNotifications.GetNotificationsQuery).Assembly);
});

// ── HTTP Client with Polly resilience ─────────────────────────────────────────
builder.Services.AddHttpClient("TalentBridgeClient")
    .AddResilienceHandler("talentbridge-resilience", (pipelineBuilder, context) =>
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
        TalentBridgeResiliencePolicies.ConfigureHttpPipeline(pipelineBuilder, logger);
    });

// ── CORS: restrict to known origins only ─────────────────────────────────────
var allowedOrigins = config["Cors:AllowedOrigins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
    ?? ["https://localhost:3000", "https://talentbridge-swa-amey.azurestaticapps.net"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("TalentBridgeCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Rate limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Global: 100 requests per minute per IP — applies to all endpoints
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Auth policy: relaxed in dev (50/15 min); tighten to 5 before prod deploy
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Environment.IsDevelopment() ? 50 : 5,
                Window = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── API versioning ────────────────────────────────────────────────────────────
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

// ── Controllers + Swagger (Bearer lock icon already configured) ───────────────
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TalentBridge API",
        Version = "v1",
        Description = "Enterprise Hiring Platform — Modular Monolith"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Startup DB migration (runs on first boot in Azure via Managed Identity) ───
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    async Task MigrateAsync<T>() where T : DbContext
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<T>();
            await db.Database.MigrateAsync();
            logger.LogInformation("[Startup] {Context} migration succeeded", typeof(T).Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Startup] {Context} migration failed — app will continue", typeof(T).Name);
        }
    }
    await MigrateAsync<IdentityDbContext>();
    await MigrateAsync<JobsDbContext>();
    await MigrateAsync<ApplicationsDbContext>();
    await MigrateAsync<CompanyDbContext>();
    await MigrateAsync<NotificationsDbContext>();
}

await DataSeeder.SeedAsync(app.Services, app.Logger);

// ── Security headers ──────────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    context.Response.Headers.Remove("X-Powered-By");
    await next();
});

// ── Pipeline ──────────────────────────────────────────────────────────────────
app.UseDefaultFiles();
app.UseStaticFiles();

// Return JSON { message } for unhandled exceptions instead of HTML developer page
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    ctx.Response.ContentType = "application/json";
    var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var ex = feature?.Error;
    var (status, msg) = ex switch
    {
        InvalidOperationException ioe => (400, ioe.Message),
        ArgumentException ae          => (400, ae.Message),
        UnauthorizedAccessException   => (401, "Unauthorized."),
        KeyNotFoundException knfe     => (404, knfe.Message),
        _                             => (500, "An unexpected error occurred.")
    };
    ctx.Response.StatusCode = status;
    await ctx.Response.WriteAsJsonAsync(new { message = msg });
}));

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TalentBridge API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("TalentBridgeCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapResilienceEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
