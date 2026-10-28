using LolStats.Data;
using LolStats.Domain.Jobs;
using LolStats.Infrastructure;
using LolStats.Riot;
using LolStats.Worker.Jobs;

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

var host = builder.Build();
host.Run();
