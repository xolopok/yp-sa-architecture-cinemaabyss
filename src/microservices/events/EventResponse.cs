namespace Events;

public sealed record EventResponse
{
    public string Status { get; init; } = "";
    public int Partition { get; init; }
    public long Offset { get; init; }
    public object? Event { get; init; }
}