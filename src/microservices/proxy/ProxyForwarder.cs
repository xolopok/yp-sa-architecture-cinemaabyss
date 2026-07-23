using System.Net.Http.Headers;

namespace Proxy;

public sealed partial class ProxyForwarder(
    ILogger<ProxyForwarder> logger,
    IHttpClientFactory httpClientFactory,
    ProxyRouter router)
{
    public const string HttpClientName = "proxy";

    // Заголовки, которые не нужно копировать вручную.
    private static readonly HashSet<string> HopByHopRequestHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host",
        "Content-Length",
        "Transfer-Encoding",
    };

    public async Task ForwardAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var targetUrl = $"{router.ResolveBaseUrl(path)}{path}{context.Request.QueryString}";

        using var request = await BuildRequestAsync(context, targetUrl);

        var client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            await CopyResponseAsync(context, response);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Upstream service unavailable for {Path}", path);
            await WriteErrorAsync(context, StatusCodes.Status502BadGateway,
                $"{{\"error\":\"Upstream service unavailable: {ex.Message}\"}}");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Upstream request timed out for {Path}", path);
            await WriteErrorAsync(context, StatusCodes.Status504GatewayTimeout,
                "{\"error\":\"Upstream request timed out\"}");
        }
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(HttpContext context, string targetUrl)
    {
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

        foreach (var header in context.Request.Headers)
        {
            if (HopByHopRequestHeaders.Contains(header.Key) ||
                header.Key.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            catch (Exception ex)
            {
                LogFailedToForwardRequestHeader(header.Key, ex);
            }
        }

        if (context.Request.ContentLength is > 0)
        {
            var buffer = new MemoryStream();
            await context.Request.Body.CopyToAsync(buffer, context.RequestAborted);
            buffer.Position = 0;

            request.Content = new StreamContent(buffer);
            if (context.Request.ContentType is not null)
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }

        return request;
    }

    private async Task CopyResponseAsync(HttpContext context, HttpResponseMessage response)
    {
        context.Response.StatusCode = (int)response.StatusCode;

        CopyHeaders(context, response.Headers);
        CopyHeaders(context, response.Content.Headers);

        // Веб-сервер (Kestrel) сам управляет кадрированием тела.
        context.Response.Headers.Remove("Transfer-Encoding");

        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    private void CopyHeaders(HttpContext context, HttpHeaders headers)
    {
        foreach (var header in headers)
        {
            if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "Failed to copy response header {HeaderKey}", header.Key);
            }
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string json)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(json);
    }

    [LoggerMessage(LogLevel.Trace, "Failed to forward request header {HeaderKey}")]
    partial void LogFailedToForwardRequestHeader(string headerKey, Exception exception);
}
