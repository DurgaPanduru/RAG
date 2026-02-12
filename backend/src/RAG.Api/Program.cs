using FluentValidation;
using Prometheus;
using RAG.Application.Commands;
using RAG.Application.Validators;
using RAG.Domain.Interfaces;
using RAG.Infrastructure.Caching;
using RAG.Infrastructure.LangChain;
using RAG.Infrastructure.Storage;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://seq:80")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "RAG Backend API", Version = "v1" });
});

// Add CORS
var corsOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new[] { "http://localhost:4200" };

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

// Add Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProcessDocumentCommand).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ProcessDocumentCommandValidator>();

// Add application services
builder.Services.AddSingleton<ILangChainService, LangChainGrpcClient>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<ISemanticCacheService, SemanticCacheService>();
builder.Services.AddSingleton<IDocumentStorageService, MinioStorageService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddRedis(redisConnection, name: "redis");

var app = builder.Build();

// Configure middleware pipeline
app.UseSerilogRequestLogging();

// Swagger in all environments for development
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG Backend API v1");
    c.RoutePrefix = "swagger";
});

// Enable CORS
app.UseCors();

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Health checks
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "RAG Backend API",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

try
{
    Log.Information("Starting RAG Backend API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Backend API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
