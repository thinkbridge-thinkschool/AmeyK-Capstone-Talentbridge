using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TalentBridge.Notifications.Infrastructure.Interfaces;

namespace TalentBridge.Notifications.Infrastructure.Relay;

public class OutboxRelayService : BackgroundService
{
    public static bool SimulateCrash { get; set; } = false;

    private readonly IOutboxRepository _outboxRepository;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly string _topicName;

    public OutboxRelayService(
        IOutboxRepository outboxRepository,
        ServiceBusClient serviceBusClient,
        IConfiguration configuration,
        ILogger<OutboxRelayService> logger)
    {
        _outboxRepository = outboxRepository;
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
        var pending = await _outboxRepository.GetPendingAsync(maxRetries: 5, ct);

        if (pending.Count == 0) return;

        _logger.LogInformation("[OutboxRelay] Processing {Count} pending outbox messages", pending.Count);

        await using var sender = _serviceBusClient.CreateSender(_topicName);

        foreach (var message in pending)
        {
            try
            {
                var sbMessage = new ServiceBusMessage(message.Payload)
                {
                    MessageId = message.Id.ToString(),
                    Subject = message.EventType
                };

                await sender.SendMessageAsync(sbMessage, ct);

                if (SimulateCrash)
                {
                    _logger.LogWarning("[OutboxRelay] CRASH SIMULATION — throwing after publish, before marking sent (proves at-least-once delivery)");
                    throw new Exception("Simulated crash after publish");
                }

                message.ProcessedAt = DateTime.UtcNow;
                await _outboxRepository.SaveAsync(message, ct);

                _logger.LogInformation("[OutboxRelay] Relayed outbox message {Id} ({EventType})", message.Id, message.EventType);
            }
            catch (Exception ex) when (!SimulateCrash || ex.Message != "Simulated crash after publish")
            {
                message.RetryCount++;
                message.Error = ex.Message;
                await _outboxRepository.SaveAsync(message, ct);

                _logger.LogError(ex, "[OutboxRelay] Failed to relay outbox message {Id} — retry count: {RetryCount}", message.Id, message.RetryCount);
            }
        }
    }
}
