using System.Reflection;
using System.Threading.RateLimiting;
using JasperFx;
using Marten;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Orion.MacroEconomics.Configuration;
using Orion.MacroEconomics.Data;
using Orion.MacroEconomics.Engine;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Engine.Interfaces.Orion.API.TradingEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Helpers;
using Orion.MacroEconomics.Interfaces;
using Orion.MacroEconomics.Providers;
using Orion.MacroEconomics.Providers.Interfaces;
using Orion.MacroEconomics.Services;
using YahooQuotesApi;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("MacroDbConnection") ?? "");
    options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;

    options.Schema.For<TradePlan>()
        .Index(x => x.Status)
        .Index(x => x.Pair)
        .Index(x => x.OpenedAt)
        .Index(x => x.ClosedAt);

    options.Schema.For<OrderRequest>()
        .Index(x => x.Status)
        .Index(x => x.Pair)
        .Index(x => x.CreatedAt);

    options.Schema.For<OrderState>()
        .Index(x => x.Status)
        .Index(x => x.Pair)
        .Index(x => x.FilledAt);
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Orion Macro Economics API",
        Version = "v1",
        Description = "An API for economic events and forex analysis.",
        Contact = new OpenApiContact
        {
            Name = "Khotso Mokhethi",
            Email = "Mokhetkc@hotmail.com",
            Url = new Uri("https://github.com/EdCharlesDiesel")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.Configure<AppConfiguration>(
    builder.Configuration.GetSection("AppConfiguration"));

builder.Services.Configure<MarketPipelineOptions>(options =>
{
    options.EnableCaching = true;
    options.CacheExpirationSeconds = 300;
    options.ValidationRetries = 2;
    options.EnableEnrichment = true;
});

builder.Services.Configure<TradingEconomicsOptions>(
    builder.Configuration.GetSection("TradingEconomics"));

builder.Services.AddSingleton(new NormalizationOptions
{
    MinimumWindowSize = 6,
    WinsorizeOutliers = true,
    WinsorizeZLimit = 4.0m
});

builder.Services.AddSingleton<YahooQuotes>(sp =>
    new YahooQuotesBuilder()
        .WithLogger(sp.GetRequiredService<ILogger<YahooQuotes>>())
        .Build());

builder.Services.AddHttpClient("FRED", client =>
{
    client.BaseAddress = new Uri("https://api.stlouisfed.org/fred/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("TradingEconomics", client =>
{
    client.BaseAddress = new Uri("https://api.tradingeconomics.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient();

builder.Services.AddScoped<IFredService, FredService>();

// builder.Services.AddScoped<IYahooMarketProvider, YahooMarketProvider>();
builder.Services.AddScoped<IDukascopyTickProvider, DukascopyTickProvider>();
builder.Services.AddScoped<ITrueFxTickProvider, TrueFxTickProvider>();
builder.Services.AddScoped<IFredMacroProvider, FredMacroProvider>();
builder.Services.AddScoped<ITradingEconomicsProvider, TradingEconomicsProvider>();

// builder.Services.AddScoped<IMarketDataFeedProvider>(sp =>
    // sp.GetRequiredService<IYahooMarketProvider>());

builder.Services.AddScoped<IMarketDataFeedProvider>(sp =>
    sp.GetRequiredService<IDukascopyTickProvider>());

builder.Services.AddScoped<IMarketDataFeedProvider>(sp =>
    sp.GetRequiredService<ITrueFxTickProvider>());

builder.Services.AddScoped<IMarketDataFeedProvider>(sp =>
    sp.GetRequiredService<IFredMacroProvider>());

builder.Services.AddScoped<IMarketDataFeedProvider>(sp =>
    sp.GetRequiredService<ITradingEconomicsProvider>());

builder.Services.AddScoped<IMarketDataStore, MarketDataStore>();
builder.Services.AddScoped<IMarketDataEngine, MarketDataEngine>();

builder.Services.AddScoped<IOrderBookProvider, OrderBookProvider>();
builder.Services.AddScoped<IAuditStorage, AuditStorage>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

builder.Services.AddScoped<ConfigurationEngine>();
builder.Services.AddScoped<ScenarioEngine>();
builder.Services.AddScoped<ExitEngine>();
builder.Services.AddScoped<FxRelativePricer>();
builder.Services.AddScoped<FxPriceSimulator>();

builder.Services.AddScoped<CurrencyStrengthModel>(_ =>
    new CurrencyStrengthModel(new List<CurrencyModel>
    {
        new()
        {
            Currency = "EUR",
            CarryWeight = 1m,
            GrowthWeight = 1m,
            InflationWeight = 1m,
            RiskWeight = 1m
        },
        new()
        {
            Currency = "USD",
            CarryWeight = 1m,
            GrowthWeight = 1m,
            InflationWeight = 1m,
            RiskWeight = 1m
        }
    }));


builder.Services.Configure<AlphaVantageOptions>(
    builder.Configuration.GetSection("AlphaVantage"));

builder.Services.Configure<GmailOptions>(
    builder.Configuration.GetSection("Gmail"));

builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("MacroDbConnection")!);
    options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;

    options.Schema.For<MarketDataDocument>()
        .Index(x => x.Provider)
        .Index(x => x.Pair)
        .Index(x => x.CreatedUtc);

    options.Schema.For<TradingSignalDocument>()
        .Index(x => x.Pair)
        .Index(x => x.Direction)
        .Index(x => x.CreatedUtc);
});

builder.Services.AddHttpClient<IAlphaVantageMarketDataProvider, AlphaVantageMarketDataProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AlphaVantageOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
 });
//
builder.Services.AddScoped<IMarketDataDocumentStore, MarketDataDocumentStore>();
builder.Services.AddScoped<IAlphaVantageSignalEngine, AlphaVantageSignalEngine>();
builder.Services.AddScoped<IGmailSignalNotificationService, GmailSignalNotificationService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));
builder.Services.AddScoped<IExecutionCostModel, SimpleExecutionCostModel>();
builder.Services.AddScoped<ILatencyModel, SimpleLatencyModel>();
builder.Services.AddScoped<ICorrelatedShockGenerator, CorrelatedShockGenerator>();
builder.Services.AddScoped<IVolatilityService, VolatilityService>();
builder.Services.AddScoped<ITradingEconomicsClient, TradingEconomicsClient>();
builder.Services.AddScoped<INewsEventService, NewsEventService>();
builder.Services.AddScoped<IMacroTransitionModel, MacroTransitionModel>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<IOrderBookExecutionService, OrderBookExecutionService>();
builder.Services.AddScoped<IIngestionValidator, IngestionValidator>();
builder.Services.AddScoped<AdvancedExecutionEngine>();
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
builder.Services.AddScoped<IMacroSimulationEngine, DynamicMacroSimulationEngine>();
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
builder.Services.AddScoped<IRegimeEngine, RegimeEngine>();
builder.Services.AddScoped<IRiskEngine, RiskEngine>();
builder.Services.AddScoped<IScenarioEngine, ScenarioEngine>();
builder.Services.AddScoped<ISentimentEngine, SentimentEngine>();
builder.Services.AddScoped<ITradeLifecycleEngine, TradeLifecycleEngine>();

 

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



builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

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

