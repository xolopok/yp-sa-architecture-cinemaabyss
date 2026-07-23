namespace Proxy;

public sealed class ProxyRouter(ProxyOptions options)
{
    private const string MoviesPrefix = "/api/movies";

    public string ResolveBaseUrl(string path) =>
        IsMoviesPath(path) && ShouldRouteToMovies()
            ? options.MoviesServiceUrl
            : options.MonolithUrl;

    private bool ShouldRouteToMovies() =>
        !options.GradualMigration || Random.Shared.Next(100) < options.MoviesMigrationPercent;

    private static bool IsMoviesPath(string path)
    {
        var span = path.AsSpan();
        if (!span.StartsWith(MoviesPrefix, StringComparison.OrdinalIgnoreCase))
            return false;
        if (span.Length == MoviesPrefix.Length)
            return true;
        return span[MoviesPrefix.Length] == '/';
    }
}
