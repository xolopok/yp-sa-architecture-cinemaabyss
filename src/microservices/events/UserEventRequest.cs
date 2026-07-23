namespace Events;

public sealed record UserEventRequest(
    int UserId,
    string Action,
    string Timestamp,
    string? Username,
    string? Email
);