using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TalentBridge.Notifications.Infrastructure.Interfaces;

namespace TalentBridge.Notifications.Infrastructure.Consumers;

public class TalentBridgeEventConsumer : BackgroundService
{
    private readonly ILogger<TalentBridgeEventConsumer> _logger;
    private readonly IProcessedMessageStore _processedMessageStore;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly string _topicName;
    private ServiceBusProcessor? _processor;

    public TalentBridgeEventConsumer(
        ServiceBusClient serviceBusClient,
        IProcessedMessageStore processedMessageStore,
        IConfiguration configuration,
        ILogger<TalentBridgeEventConsumer> logger)
    {
        _serviceBusClient = serviceBusClient;
        _processedMessageStore = processedMessageStore;
        _logger = logger;
        _topicName = configuration["ServiceBus:TopicName"] ?? "talentbridge-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _serviceBusClient.CreateProcessor(_topicName, "notifications", new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        _logger.LogInformation("[Notifications] Consumer started — listening on {Topic}/notifications", _topicName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }

        _logger.LogInformation("[Notifications] Consumer stopping cleanly");

        await _processor.StopProcessingAsync();
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        var subject = args.Message.Subject;

        if (await _processedMessageStore.IsProcessedAsync(messageId))
        {
            _logger.LogWarning("[Notifications] Message {MessageId} already processed — skipping (idempotent)", messageId);
            await args.CompleteMessageAsync(args.Message);
            return;
        }

        try
        {
            switch (subject)
            {
                case "ApplicationSubmitted":
                    _logger.LogInformation("[Notifications] SIMULATE: Sending candidate application confirmation email for message {MessageId}", messageId);
                    break;
                case "ApplicationAccepted":
                    _logger.LogInformation("[Notifications] SIMULATE: Sending acceptance email to candidate for message {MessageId}", messageId);
                    break;
                case "ApplicationRejected":
                    _logger.LogInformation("[Notifications] SIMULATE: Sending rejection email with reason to candidate for message {MessageId}", messageId);
                    break;
                case "JobPublished":
                    _logger.LogInformation("[Notifications] SIMULATE: Sending job alert to matching candidates for message {MessageId}", messageId);
                    break;
                default:
                    _logger.LogWarning("[Notifications] Unknown event type: {Subject}", subject);
                    break;
            }

            await _processedMessageStore.MarkProcessedAsync(messageId);
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Notifications] Error processing message {MessageId} — abandoning for retry", messageId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "[Notifications] Service Bus error on {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _processor?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose();
    }
}
