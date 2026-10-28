using LolStats.Api.Filters;
using LolStats.Domain.Abstractions;
using LolStats.Domain.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace LolStats.Api.Controllers.Dev;

// Enqueues jobs by hand so the Api -> Hangfire -> Worker path can be checked from Postman.
[ApiController]
[DevelopmentOnly]
[Route("dev/jobs")]
public sealed class JobsDebugController(IJobQueue jobs) : ControllerBase
{
    // POST dev/jobs/refresh/{puuid}?delaySeconds=10
    [HttpPost("refresh/{puuid}")]
    public IActionResult Refresh(string puuid, [FromQuery] int? delaySeconds = null)
    {
        var job = new RefreshPlayer(puuid);
        var jobId = delaySeconds is null
            ? jobs.Enqueue(job)
            : jobs.Schedule(job, TimeSpan.FromSeconds(delaySeconds.Value));

        return Accepted(new { jobId });
    }
}
