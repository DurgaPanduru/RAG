using Microsoft.AspNetCore.RateLimiting;
using Prometheus;
using RAG.Gateway.Middleware;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://seq:80")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Configure CORS
var corsOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Rate Limiting
var rateLimitPerMinute = builder.Configuration.GetValue<int>("RateLimiting:RequestsPerMinute", 100);
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientId = context.Request.Headers["X-Client-Id"].ToString()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(clientId, _ =>
            new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = rateLimitPerMinute,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Configure JWT Authentication (optional, for future use)
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.Authority = builder.Configuration["Jwt:Authority"];
//         options.Audience = builder.Configuration["Jwt:Audience"];
//     });

var app = builder.Build();

// Configure middleware pipeline
app.UseSerilogRequestLogging();

// Add custom correlation ID middleware
app.UseMiddleware<CorrelationIdMiddleware>();

// Enable CORS
app.UseCors();

// Enable rate limiting
app.UseRateLimiter();

// Enable Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Enable authentication & authorization (if configured)
// app.UseAuthentication();
// app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// Map reverse proxy
app.MapReverseProxy();

app.MapGet("/", () => Results.Ok(new
{
    service = "RAG API Gateway",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow
}));

try
{
    Log.Information("Starting RAG API Gateway");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
