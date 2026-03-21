using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using NotificationService.Workers;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "NotificationService")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] NotificationService - {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

// ── Background Worker ────────────────────────────────────────────
builder.Services.AddHostedService<NotificationWorker>();

// ── Controllers (for health/metrics endpoints) ───────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── OpenTelemetry ────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName: "NotificationService"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opts =>
            opts.Endpoint = new Uri(builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317")))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName: "NotificationService"))
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

// ── Health Checks ────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString:
        $"amqp://{builder.Configuration["RabbitMQ:Username"] ?? "devops"}:{builder.Configuration["RabbitMQ:Password"] ?? "devops123"}@{builder.Configuration["RabbitMQ:Host"] ?? "localhost"}:5672");

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────────
app.UseSerilogRequestLogging();

app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
