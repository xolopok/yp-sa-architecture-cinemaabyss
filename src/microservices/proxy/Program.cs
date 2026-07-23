using Proxy;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8000";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddHttpClient(ProxyForwarder.HttpClientName)
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddSingleton(ProxyOptions.FromEnvironment());
builder.Services.AddSingleton<ProxyRouter>();
builder.Services.AddSingleton<ProxyForwarder>();

var app = builder.Build();

app.MapGet("/health", () => Results.Text("Strangler Fig Proxy is healthy"));
app.MapFallback((HttpContext context, ProxyForwarder forwarder) => forwarder.ForwardAsync(context));

app.Run();
