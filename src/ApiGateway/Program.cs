using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "ApiGateway")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] ApiGateway - {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

// ── YARP Reverse Proxy ───────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── OpenTelemetry ────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName: "ApiGateway"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opts =>
            opts.Endpoint = new Uri(builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317")))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName: "ApiGateway"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// ── Health Checks ────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────────
app.UseSerilogRequestLogging();

app.MapReverseProxy();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
