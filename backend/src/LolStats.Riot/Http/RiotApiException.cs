using System.Net;

namespace LolStats.Riot.Http;

// Non-success response that is not 404 or 429, after in-process retries. Lets the job fail
// so Hangfire retries it with backoff.
public sealed class RiotApiException(HttpStatusCode statusCode, string method)
    : Exception($"Riot {method} returned {(int)statusCode} {statusCode}.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Method { get; } = method;
}