using LolStats.Worker;
using LolStats.Data;
using LolStats.Riot;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddLolStatsData(
    builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'Postgres' is missing."));
builder.Services.AddLolStatsRiot();

var host = builder.Build();
host.Run();
