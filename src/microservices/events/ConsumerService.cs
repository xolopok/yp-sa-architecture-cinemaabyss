using Confluent.Kafka;

namespace Events;

public sealed partial class ConsumerService(ILogger<ConsumerService> logger) : BackgroundService
{
    private const string GroupId = "events-service";
    
    private static readonly string[] Topics = ["movie-events", "user-events", "payment-events"];
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly string kafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? "kafka:9092";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    private void Consume(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = CreateConsumer();
                consumer.Subscribe(Topics);
                LogStarted(string.Join(", ", Topics));

                ConsumeLoop(consumer, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Failed, rebuilding consumer in {Delay}", RetryDelay);
                stoppingToken.WaitHandle.WaitOne(RetryDelay);
            }
        }
    }

    private void ConsumeLoop(IConsumer<Ignore, string> consumer, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                LogEventConsumed(result.Topic, result.Partition, result.Offset, result.Message.Key, result.Message.Value);
            }
            catch (ConsumeException ex)
            {
                logger.LogWarning("Failed to consume, retrying in {Delay}: {Message}", RetryDelay, ex.Message);
                stoppingToken.WaitHandle.WaitOne(RetryDelay);
            }
        }
    }

    private IConsumer<Ignore, string> CreateConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaBrokers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        return new ConsumerBuilder<Ignore, string>(config).Build();
    }
    
    [LoggerMessage(LogLevel.Information, "Started, subscribed to {Topics}")]
    partial void LogStarted(string topics);

    [LoggerMessage(LogLevel.Information,
        "Consumed event: Topic={Topic} Partition={Partition} Offset={Offset} Key={Key} Value={Value}")]
    partial void LogEventConsumed(string topic, Partition partition, Offset offset, Ignore key, string value);
}