using LolStats.Api.Filters;
using LolStats.Domain.Common;
using LolStats.Domain.Matches;
using LolStats.Domain.Riot;
using Microsoft.AspNetCore.Mvc;

namespace LolStats.Api.Controllers.Dev;

// Calls Riot directly through IRiotGateway so each client and mapper can be checked from Postman.
// This breaks "a user request never waits for Riot" on purpose, which is why it only exists in Development.
[ApiController]
[DevelopmentOnly]
[Route("dev/riot")]
public sealed class RiotDebugController(IRiotGateway riot) : ControllerBase
{
    // GET dev/riot/account/Smart Shroom/EUNE
    [HttpGet("account/{gameName}/{tagLine}")]
    public Task<IActionResult> GetAccount(string gameName, string tagLine, CancellationToken ct) =>
        Call(async () => RiotId.TryCreate(gameName, tagLine, out var riotId)
            ? ToResult(await riot.GetAccountByRiotIdAsync(riotId, ct))
            : BadRequest("Invalid Riot ID."));

    [HttpGet("summoner/{puuid}")]
    public Task<IActionResult> GetSummoner(string puuid, CancellationToken ct) =>
        Call(async () => ToResult(await riot.GetSummonerAsync(puuid, ct)));

    [HttpGet("league/{puuid}")]
    public Task<IActionResult> GetLeague(string puuid, CancellationToken ct) =>
        Call(async () => Ok(await riot.GetLeagueEntriesAsync(puuid, ct)));

    // GET dev/riot/matches/{puuid}/ids?queue=420&count=10&startTime=2026-10-01T00:00:00Z
    [HttpGet("matches/{puuid}/ids")]
    public Task<IActionResult> GetMatchIds(
        string puuid,
        [FromQuery] QueueId queue = QueueId.RankedSolo,
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] int start = 0,
        [FromQuery] int count = 10,
        CancellationToken ct = default) =>
        Call(async () => Ok(await riot.GetMatchIdsAsync(puuid, new MatchIdsQuery(queue, startTime, start, count), ct)));

    [HttpGet("matches/{matchId}")]
    public Task<IActionResult> GetMatch(string matchId, CancellationToken ct) =>
        Call(async () => ToResult(await riot.GetMatchAsync(matchId, ct)));

    private IActionResult ToResult<T>(T? value) where T : class =>
        value is null ? NotFound() : Ok(value);

    // A rate-limited call becomes a proper 429 instead of a 500.
    private async Task<IActionResult> Call(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (RiotRateLimitException ex)
        {
            Response.Headers.RetryAfter = ((int)Math.Ceiling(ex.RetryAfter.TotalSeconds)).ToString();
            return Problem(ex.Message, statusCode: StatusCodes.Status429TooManyRequests);
        }
    }
}
