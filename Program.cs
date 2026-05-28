using System.Reflection;
using Orion.MacroEconomics.Extensions;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services
    .AddAllServices(configuration);

var app = builder.Build();

// Configure the middleware pipeline
// app.ConfigurePipeline();

app.Run();