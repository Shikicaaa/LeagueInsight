namespace LolStats.Domain.Jobs;

public enum JobPriority
{
    P0 = 0, // What user wants now
    P1, // His matches, "load more"
    P2, // analytics
    P3 // database getting bigger (crawl) 
}