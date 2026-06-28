using LearnPath.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Check()
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");

            return Ok(new
            {
                Status      = "Healthy",
                Database    = "Connected",
                Timestamp   = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable(
                    "ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Version     = "1.0.0",
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                Status    = "Unhealthy",
                Database  = "Disconnected",
                Error     = ex.Message,
                Timestamp = DateTime.UtcNow,
            });
        }
    }
}
