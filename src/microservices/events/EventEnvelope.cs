namespace Events;

public sealed record EventEnvelope<T>
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "";
    public string Timestamp { get; init; } = "";
    public T? Payload { get; init; }
}