using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.OpenApi.Models;
using TalentBridge.API.Resilience;
using TalentBridge.Applications.Application;
using TalentBridge.Applications.Infrastructure;
using TalentBridge.Identity.Infrastructure;
using TalentBridge.Jobs.Application;
using TalentBridge.Jobs.Infrastructure;
using TalentBridge.Notifications.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ── Module registrations ──────────────────────────────────────────────────────
builder.Services.AddIdentityModule(config);
builder.Services.AddJobsInfrastructure(config);
builder.Services.AddJobsApplication();
builder.Services.AddApplicationsInfrastructure(config);
builder.Services.AddApplicationsApplication();
builder.Services.AddNotificationsModule(config);

// ── Azure clients ─────────────────────────────────────────────────────────────
var sbConnection = config["ServiceBus:ConnectionString"];
if (!string.IsNullOrWhiteSpace(sbConnection) && sbConnection != "SET_IN_KEYVAULT")
    builder.Services.AddSingleton(new ServiceBusClient(sbConnection));
else
    builder.Services.AddSingleton(new ServiceBusClient("Endpoint=sb://placeholder.servicebus.windows.net/;SharedAccessKeyName=placeholder;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="));

var storageConnection = config["Storage:ConnectionString"];
if (!string.IsNullOrWhiteSpace(storageConnection) && storageConnection != "SET_IN_KEYVAULT")
    builder.Services.AddSingleton(new BlobServiceClient(storageConnection));
else
    builder.Services.AddSingleton(new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=placeholder;AccountKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==;EndpointSuffix=core.windows.net"));

// ── Caching ───────────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHybridCache();

// ── MediatR (scan all module assemblies) ─────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(TalentBridge.Identity.Application.Commands.Login.LoginCommand).Assembly,
        typeof(TalentBridge.Jobs.Application.Commands.PostJob.PostJobCommand).Assembly,
        typeof(TalentBridge.Applications.Application.Commands.Apply.ApplyCommand).Assembly);
});

// ── HTTP Client with Polly resilience ─────────────────────────────────────────
builder.Services.AddHttpClient("TalentBridgeClient")
    .AddResilienceHandler("talentbridge-resilience", (pipelineBuilder, context) =>
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
        TalentBridgeResiliencePolicies.ConfigureHttpPipeline(pipelineBuilder, logger);
    });

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
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

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TalentBridge API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapResilienceEndpoints();

app.Run();
