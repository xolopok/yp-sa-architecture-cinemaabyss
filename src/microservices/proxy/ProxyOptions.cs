namespace Proxy;

public sealed record ProxyOptions
{
    public required string MonolithUrl { get; init; }
    public required string MoviesServiceUrl { get; init; }
    public required bool GradualMigration { get; init; }
    public required int MoviesMigrationPercent { get; init; }

    public static ProxyOptions FromEnvironment() => new()
    {
        MonolithUrl = ReadUrl("MONOLITH_URL", "http://monolith:8080"),
        MoviesServiceUrl = ReadUrl("MOVIES_SERVICE_URL", "http://movies-service:8081"),
        GradualMigration = bool.TryParse(Environment.GetEnvironmentVariable("GRADUAL_MIGRATION"), out var gm) && gm,
        MoviesMigrationPercent = int.TryParse(Environment.GetEnvironmentVariable("MOVIES_MIGRATION_PERCENT"), out var mp) ? mp : 50,
    };

    private static string ReadUrl(string name, string fallback) =>
        (Environment.GetEnvironmentVariable(name) ?? fallback).TrimEnd('/');
}
