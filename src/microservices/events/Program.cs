using System.Text.Json;
using Confluent.Kafka;
using Events;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8082";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

var kafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? "kafka:9092";

var producerConfig = new ProducerConfig { BootstrapServers = kafkaBrokers };
builder.Services.AddSingleton(new ProducerBuilder<string, string>(producerConfig).Build());

builder.Services.AddHostedService<ConsumerService>();
builder.Services.AddHostedService<ProducerLifetimeService>();

var app = builder.Build();

app.MapGet("/api/events/health", () => Results.Json(new { status = true }));

app.MapPost("/api/events/movie", async (MovieEventRequest req, IProducer<string, string> producer) =>
{
    if (string.IsNullOrEmpty(req.Title) || string.IsNullOrEmpty(req.Action))
        return Results.BadRequest(new { error = "title and action are required" });

    var eventId = $"movie-{req.MovieId}-{req.Action}";
    return await HandleEvent("movie", "movie-events", eventId, req, producer);
});

app.MapPost("/api/events/user", async (UserEventRequest req, IProducer<string, string> producer) =>
{
    if (string.IsNullOrEmpty(req.Action) || string.IsNullOrEmpty(req.Timestamp))
        return Results.BadRequest(new { error = "action and timestamp are required" });

    var eventId = $"user-{req.UserId}-{req.Action}";
    return await HandleEvent("user", "user-events", eventId, req, producer);
});

app.MapPost("/api/events/payment", async (PaymentEventRequest req, IProducer<string, string> producer) =>
{
    if (string.IsNullOrEmpty(req.Status) || string.IsNullOrEmpty(req.Timestamp))
        return Results.BadRequest(new { error = "status and timestamp are required" });

    var eventId = $"payment-{req.PaymentId}-{req.Status}";
    return await HandleEvent("payment", "payment-events", eventId, req, producer);
});

app.Run();

return;

static async Task<IResult> HandleEvent<T>(
    string eventType,
    string topic,
    string eventId,
    T payload,
    IProducer<string, string> producer)
{
    var eventData = new EventEnvelope<T>
    {
        Id = eventId,
        Type = eventType,
        Timestamp = DateTime.UtcNow.ToString("o"),
        Payload = payload
    };

    var messageValue = JsonSerializer.Serialize(eventData, EventJsonOptions.Value);

    var message = new Message<string, string>
    {
        Key = eventId,
        Value = messageValue
    };

    var deliveryResult = await producer.ProduceAsync(topic, message);

    return Results.Json(new EventResponse
    {
        Status = "success",
        Partition = deliveryResult.Partition.Value,
        Offset = deliveryResult.Offset.Value,
        Event = eventData
    }, statusCode: 201);
}