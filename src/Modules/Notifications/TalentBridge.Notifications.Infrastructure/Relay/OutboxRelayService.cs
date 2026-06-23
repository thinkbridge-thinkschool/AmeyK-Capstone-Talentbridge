using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TalentBridge.Notifications.Infrastructure.Interfaces;

namespace TalentBridge.Notifications.Infrastructure.Relay;

public class OutboxRelayService : BackgroundService
{
    public static bool SimulateCrash { get; set; } = false;

    // ActivitySource name must match the source registered in Program.cs AddSource()
    private static readonly ActivitySource _activitySource =
        new("TalentBridge.Application", "1.0.0");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly string _topicName;

    public OutboxRelayService(
        IServiceScopeFactory scopeFactory,
        ServiceBusClient serviceBusClient,
        IConfiguration configuration,
        ILogger<OutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _serviceBusClient = serviceBusClient;
        _logger = logger;
        _topicName = configuration["ServiceBus:TopicName"] ?? "talentbridge-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[OutboxRelay] Service started — polling every 5 seconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "[OutboxRelay] Unexpected error in polling loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ContinueWith(_ => { });
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var pending = await outboxRepository.GetPendingAsync(ct);
        var _outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var pending = await _outboxRepository.GetPendingAsync(ct);

        if (pending.Count == 0) return;

        _logger.LogInformation("[OutboxRelay] Processing {Count} pending outbox messages", pending.Count);

        await using var sender = _serviceBusClient.CreateSender(_topicName);

        foreach (var message in pending)
        {
            using var activity = _activitySource.StartActivity("OutboxRelay.Publish");
            activity?.SetTag("messageType", message.Type);
            activity?.SetTag("messageId", message.Id.ToString());

            try
            {
                var sbMessage = new ServiceBusMessage(message.Payload)
                {
                    MessageId = message.Id.ToString(),
                    Subject = message.Type
                };

                await sender.SendMessageAsync(sbMessage, ct);

                if (SimulateCrash)
                {
                    _logger.LogWarning("[OutboxRelay] CRASH SIMULATION — throwing after publish, before marking sent (proves at-least-once delivery)");
                    throw new Exception("Simulated crash after publish");
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
                await outboxRepository.SaveAsync(message, ct);

                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("[OutboxRelay] Relayed outbox message {Id} ({Type})", message.Id, message.Type);
            }
            catch (Exception ex) when (!SimulateCrash || ex.Message != "Simulated crash after publish")
            {
                await outboxRepository.SaveAsync(message, ct);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                await _outboxRepository.SaveAsync(message, ct);

                _logger.LogError(ex, "[OutboxRelay] Failed to relay outbox message {Id}", message.Id);
            }
        }
    }
}
