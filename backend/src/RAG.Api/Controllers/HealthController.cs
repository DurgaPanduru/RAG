using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IConnectionMultiplexer redis,
        ILogger<HealthController> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> GetHealth()
    {
        var health = new HealthStatus
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        };

        var services = new Dictionary<string, string>();

        // Check Redis
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            services["redis"] = "healthy";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed");
            services["redis"] = "unhealthy";
            health.Status = "degraded";
        }

        // Check LangChain service (would need to implement a health check endpoint)
        services["langchain"] = "unknown";

        // Check MinIO (would need to implement)
        services["minio"] = "unknown";

        // Check Vector DB (would need to implement)
        services["vectordb"] = "unknown";

        health.Services = services;

        if (health.Status == "unhealthy")
            return StatusCode(503, health);

        return Ok(health);
    }
}

public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, string> Services { get; set; } = new();
}
