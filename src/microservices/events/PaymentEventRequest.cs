namespace Events;

public sealed record PaymentEventRequest(
    int PaymentId,
    int UserId,
    double Amount,
    string Status,
    string Timestamp,
    string? MethodType
);