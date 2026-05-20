using System.Reflection;
using System.Threading.RateLimiting;
using JasperFx;
using Marten;
using Microsoft.OpenApi.Models;
using Orion.MacroEconomics.Configurations;
using Orion.MacroEconomics.Engine;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Engine.Interfaces.Orion.API.TradingEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Extensions;
using Orion.MacroEconomics.Helpers;
using Orion.MacroEconomics.Helpers.Interfaces;
using Orion.MacroEconomics.Interfaces;
using Orion.MacroEconomics.Jobs;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Repository;
using Orion.MacroEconomics.Repository.Interfaces;
using Orion.MacroEconomics.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Orion Macro Economics API",
        Version     = "v1",
        Description = "An API for economic events and forex analysis.",
        Contact = new OpenApiContact
        {
            Name  = "Khotso Mokhethi",
            Email = "Mokhetkc@hotmail.com",
            Url   = new Uri("https://github.com/EdCharlesDiesel")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url  = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});
builder.Services.Configure<AppConfiguration>(
    configuration.GetSection("AppConfiguration"));

builder.Services.Configure<MarketPipelineOptions>(options =>
{
    options.EnableCaching          = true;
    options.CacheExpirationSeconds = 300;
    options.ValidationRetries      = 2;
    options.EnableEnrichment       = true;
});

// builder.Services.Configure<GmailOptions>(
//     configuration.GetSection("Gmail"));


builder.Services.AddSingleton(new NormalizationOptions
{
    MinimumWindowSize = 6,
    WinsorizeOutliers = true,
    WinsorizeZLimit   = 4.0m
});

// Currency strength model
builder.Services.AddSingleton<CurrencyStrengthModel>(_ =>
    new CurrencyStrengthModel(new List<CurrencyModel>
    {
        new() { Currency = "EUR", CarryWeight = 1m, GrowthWeight = 1m, InflationWeight = 1m, RiskWeight = 1m },
        new() { Currency = "USD", CarryWeight = 1m, GrowthWeight = 1m, InflationWeight = 1m, RiskWeight = 1m }
    }));

// ── HTTP Clients ──────────────────────────────────────────────────────────────

// Massive Client (Forex API)
builder.Services.AddHttpClient<MassiveClient>(client =>
{
    client.BaseAddress = new Uri(configuration["Massive:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration["Massive:ApiKey"]}");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});


builder.Services.AddScoped<IMarketDataRepository, MarketDataRepository>();
builder.Services.AddScoped<IMassiveForexRepository, MassiveForexRepository>();
builder.Services.AddScoped<TradePlanFactory>();
builder.Services.AddScoped<IAuditStorage, AuditStorage>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IExecutionCostModel, SimpleExecutionCostModel>();
builder.Services.AddScoped<ILatencyModel, SimpleLatencyModel>();
builder.Services.AddScoped<ICorrelatedShockGenerator, CorrelatedShockGenerator>();
builder.Services.AddScoped<IVolatilityService, VolatilityService>();
builder.Services.AddScoped<INewsEventService, NewsEventService>();
builder.Services.AddScoped<IMacroTransitionModel, MacroTransitionModel>();
builder.Services.AddScoped<IOrderBookExecutionService, OrderBookExecutionService>();
builder.Services.AddScoped<IIngestionValidator, IngestionValidator>();
builder.Services.AddScoped<FxRelativePricer>();
builder.Services.AddScoped<FxPriceSimulator>();
builder.Services.AddScoped<IMarketDataEngine, MarketDataEngine>();
builder.Services.AddScoped<IAdvancedExecutionEngine, AdvancedExecutionEngine>();
builder.Services.AddScoped<IAlertEngine, AlertEngine>();
builder.Services.AddScoped<IAlphaEngine, AlphaEngine>();
builder.Services.AddScoped<IAuditTrailEngine, AuditTrailEngine>();
builder.Services.AddScoped<IBacktestEngine, BacktestEngine>();
builder.Services.AddScoped<ICircuitBreakerEngine, CircuitBreakerEngine>();
builder.Services.AddScoped<IComplianceEngine, ComplianceEngine>();
builder.Services.AddScoped<ICorrelationEngine, CorrelationEngine>();
builder.Services.AddScoped<IConfigurationEngine, ConfigurationEngine>();
builder.Services.AddScoped<IDataQualityEngine, DataQualityEngine>();
builder.Services.AddScoped<IEconomicCalendarRiskEngine, EconomicCalendarRiskEngine>();
builder.Services.AddScoped<IExecutionEngine, ExecutionEngine>();
builder.Services.AddScoped<IExitEngine, ExitEngine>();
builder.Services.AddScoped<IFxPricingEngine, FxPricingEngine>();
builder.Services.AddScoped<IHedgingEngine, HedgingEngine>();
builder.Services.AddScoped<ILiquidityEngine, LiquidityEngine>();
builder.Services.AddScoped<IMarketReplayEngine, MarketReplayEngine>();
builder.Services.AddScoped<IModelValidationEngine, ModelValidationEngine>();
builder.Services.AddScoped<IMonteCarloEngine, MonteCarloEngine>();
builder.Services.AddScoped<INormalizationEngine, NormalizationEngine>();
builder.Services.AddScoped<IOrderManagementEngine, OrderManagementEngine>();
builder.Services.AddScoped<IPerformanceAnalyticsEngine, PerformanceAnalyticsEngine>();
builder.Services.AddScoped<IPortfolioEngine, PortfolioEngine>();
builder.Services.AddScoped<IPositionSizingEngine, PositionSizingEngine>();
builder.Services.AddScoped<IProbabilisticScenarioEngine, ProbabilisticScenarioEngine>();
builder.Services.AddScoped<IRealBacktestEngine, RealBacktestEngine>();
builder.Services.AddScoped<IRealTimeRiskEngine, RealTimeRiskEngine>();
builder.Services.AddScoped<IRiskEngine, RiskEngine>();
builder.Services.AddScoped<IRegimeEngine, RegimeEngine>();
builder.Services.AddScoped<IScenarioEngine, ScenarioEngine>();
builder.Services.AddScoped<ISentimentEngine, SentimentEngine>();
builder.Services.AddScoped<ITradeLifecycleEngine, TradeLifecycleEngine>();
builder.Services.AddScoped<AdvancedExecutionEngine>();
builder.Services.AddScoped<ConfigurationEngine>();
builder.Services.AddScoped<ScenarioEngine>();
builder.Services.AddScoped<ExitEngine>();

// ── Background Services ───────────────────────────────────────────────────────
builder.Services.AddHostedService<MassiveBackgroundService>();

// Note: If you have both MassiveBackgroundService and MassiveDataCollectionService,
// you should choose one to avoid duplicate data collection. Remove or comment out:
// builder.Services.AddHostedService<MassiveDataCollectionService>();

// ── Generic Repository ────────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));

// ── CORS ───────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit       = 100,
                Window            = TimeSpan.FromMinutes(1)
            }));
});

// Register repositories
builder.Services.AddScoped<IForexEventRepository, ForexEventRepository>();
builder.Services.AddScoped<IForexNewsRepository, ForexNewsRepository>();

// Register services
builder.Services.AddHttpClient<ForexEventScraperService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "ForexNewsService/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register engine
builder.Services.AddScoped<INewsEngine, NewsEngine>();

// Register background service
builder.Services.AddHostedService<ForexDataBackgroundService>();

// ── Marten/PostgreSQL ─────────────────────────────────────────────────────────
builder.Services.AddMarten(options =>
{
    options.Connection(configuration.GetConnectionString("OrionMacroDbConnection")!);
    options.AutoCreateSchemaObjects = AutoCreate.All;

    // Register your document types
    options.Schema.For<MarketDataSnapshot>()
        .Identity(x => x.Id);

    options.Schema.For<CandleDocument>()
        .Identity(x => x.Id);

    options.Schema.For<TradePlan>()
        .Identity(x => x.Id);

    options.Schema.For<MacroSnapshotDocument>()
        .Identity(x => x.Id);

    options.Schema.For<EconomySeriesDocument>()
        .Identity(x => x.Id);

    options.Schema.For<ForexSnapshotDocument>()
        .Identity(x => x.Id);

    options.Schema.For<ForexTickerDocument>()
        .Identity(x => x.Ticker);

    options.Schema.For<ForexQuoteDocument>()
        .Identity(x => x.Id);

    options.Schema.For<ConversionDocument>()
        .Identity(x => x.Id);

    options.Schema.For<IndicatorDocument>()
        .Identity(x => x.Id);

    options.Schema.For<MarketStatusDocument>()
        .Identity(x => x.Id);

    options.Schema.For<MarketHolidayDocument>()
        .Identity(x => x.Id);
});

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AngularApp");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();