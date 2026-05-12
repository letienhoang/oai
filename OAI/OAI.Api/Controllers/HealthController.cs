using Microsoft.AspNetCore.Mvc;

namespace OAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : Controller
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "OAI.Api"
        });
    }
}