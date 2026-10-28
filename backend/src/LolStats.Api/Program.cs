using System.Text.Json.Serialization;
using Hangfire;
using LolStats.Data;
using LolStats.Infrastructure;
using LolStats.Riot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLolStatsData(
    builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'Postgres' is missing."));
builder.Services.AddLolStatsRiot();
// Api only enqueues; jobs run in the Worker.
builder.Services.AddLolStatsJobs(
    builder.Configuration.GetConnectionString("Hangfire")
        ?? throw new InvalidOperationException("Connection string 'Hangfire' is missing."));

// Enums as names ("MIDDLE", "RankedSolo") instead of numbers, in responses and query strings.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Without an authorization filter the dashboard only answers requests from localhost.
    app.MapHangfireDashboard("/hangfire");
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();