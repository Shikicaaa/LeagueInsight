namespace LolStats.Domain.Common;

public sealed record RiotId
{
    public string GameName {get; }
    public string TagLine {get;}

    private RiotId(string gameName, string tagLine)
    {
        this.GameName = gameName;
        this.TagLine = tagLine;
    }

    public string Normalized => $"{Normalize(GameName)}#{Normalize(TagLine)}";

    public override string ToString() => $"{GameName}#{TagLine}";

    public static RiotId Create(string gameName, string tagLine) =>
        TryCreate(gameName, tagLine, out var riotId)
            ? riotId
            : throw new ArgumentException("Invalid Riot ID");

    public static bool TryCreate(string? gameName, string? tagLine, out RiotId riotId)
    {
        riotId = null!;
        if (gameName is null || tagLine is null)
            return false;
        gameName = gameName.Trim();
        tagLine = tagLine.Trim().TrimStart('#');
        if (gameName.Length is < 3 or > 16 || tagLine.Length is < 3 or > 5)
            return false;
        riotId = new RiotId(gameName, tagLine);
        return true;
    }

/// <summary>
/// Tries to parse a Riot ID from a string in the format "GameName#Tagline".
/// </summary>

    public static bool TryParse(string? value, out RiotId riotId)
    {
        riotId = null!;
        var hash = value?.LastIndexOf('#') ?? -1;
        return hash > 0 && TryCreate(value![..hash], value[(hash + 1)..], out riotId);
    }

/// <summary>
/// Tries to parse a Riot ID from a string in the format "GameName-Tagline".
/// </summary>
    public static bool TryParseSlug(string? slug, out RiotId riotId)
    {
        riotId = null!;
        var dash = slug?.LastIndexOf('-') ?? -1;
        return dash > 0 && TryCreate(slug![..dash], slug[(dash + 1)..], out riotId);
    }

    private static string Normalize(string value) =>
        string.Concat(value.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
}