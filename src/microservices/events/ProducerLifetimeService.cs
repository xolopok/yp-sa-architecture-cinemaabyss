using Confluent.Kafka;

namespace Events;

public sealed class ProducerLifetimeService(IProducer<string, string> producer, IHostApplicationLifetime lifetime) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime.ApplicationStopping.Register(() =>
        {
            try
            {
                producer.Flush(TimeSpan.FromSeconds(10));
            }
            catch
            {
                // игнорируем
            }
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}