namespace Events;

public sealed record MovieEventRequest(
    int MovieId,
    string Title,
    string Action,
    int? UserId,
    double? Rating,
    string[]? Genres,
    string? Description
);