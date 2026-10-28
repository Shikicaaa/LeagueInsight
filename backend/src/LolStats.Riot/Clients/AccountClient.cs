using LolStats.Riot.DTOs;

namespace LolStats.Riot.Clients;

internal sealed class AccountClient(HttpClient http) : RiotClientBase(http)
{
    public Task<AccountDto?> GetByRiotIdAsync(string gameName, string tagLine, CancellationToken ct)
        => GetAsync<AccountDto>(
            $"riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}",
            "account-v1.by-riot-id",
            ct); 
    
    public Task<AccountDto?> GetByPuuidAsync(string puuid, CancellationToken ct)
        => GetAsync<AccountDto>(
            $"riot/account/v1/accounts/by-puuid/{Uri.EscapeDataString(puuid)}",
            "account-v1.by-puuid",
            ct
        );
}