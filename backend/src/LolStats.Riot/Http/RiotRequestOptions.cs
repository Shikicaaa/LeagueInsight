namespace LolStats.Riot.Http;

internal static class RiotRequestOptions
{
    public static readonly HttpRequestOptionsKey<string> Method = new("riot-method");
}