using LolStats.Data;
using LolStats.Domain.Jobs;
using LolStats.Infrastructure;
using LolStats.Riot;
using LolStats.Worker.Jobs;
using LolStats.Worker.Ingestion;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLolStatsData(
    builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'Postgres' is missing."));
builder.Services.AddLolStatsRiot();
builder.Services.AddLolStatsJobs(
    builder.Configuration.GetConnectionString("Hangfire")
        ?? throw new InvalidOperationException("Connection string 'Hangfire' is missing."));
builder.Services.AddLolStatsJobServer();
builder.Services.AddLolStatsValkey(
    builder.Configuration["Cache:ConnectionString"]
        ?? throw new InvalidOperationException("Cache connection string is missing.")
);

// Job handlers: one per JobRequest type.
builder.Services.AddScoped<IJobHandler<RefreshPlayer>, RefreshPlayerJob>();
builder.Services.AddScoped<IJobHandler<FetchMatches>, FetchMatchesJob>();
builder.Services.AddScoped<IJobHandler<LoadMoreMatches>, LoadMoreMatchesJob>();

// Shared by every job that pulls matches; scoped so it shares the job's DbContext.
builder.Services.AddScoped<MatchIngestor>();

var host = builder.Build();
host.Run();
