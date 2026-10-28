using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LolStats.Riot.Http;

namespace LolStats.Riot.Clients;

internal abstract class RiotClientBase(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    protected async Task<T?> GetAsync<T>(string path, string method, CancellationToken ct) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Options.Set(RiotRequestOptions.Method, method);

        using var response = await http.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        if (!response.IsSuccessStatusCode)
            throw new RiotApiException(response.StatusCode, method);
        
        return await response.Content.ReadFromJsonAsync<T>(Json, ct);
    }
}