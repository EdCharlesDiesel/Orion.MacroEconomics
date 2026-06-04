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
using Orion.MacroEconomics.Helpers;
using Orion.MacroEconomics.Helpers.Interfaces;
using Orion.MacroEconomics.Interfaces;
using Orion.MacroEconomics.Jobs;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Providers;
using Orion.MacroEconomics.Repository;
using Orion.MacroEconomics.Repository.Interfaces;
using Orion.MacroEconomics.Services;
using Orion.MacroEconomics.Strategies;
using Quartz;
using IngestionValidator = Orion.MacroEconomics.Helpers.IngestionValidator;

namespace Orion.MacroEconomics.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddAllServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddMemoryCache();

        services.AddSwaggerServices();
        services.AddConfigurationServices(configuration);
        services.AddHttpClients(configuration);
        services.AddBusinessServices();
        services.AddEngineServices();
        services.AddRepositoryServices();
        services.AddBackgroundServices();
        services.AddCorsServices();
        services.AddRateLimitingServices();
        services.AddMartenDatabase(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }

    private static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
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

        return services;
    }

    private static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppConfiguration>(configuration.GetSection("AppConfiguration"));

        services.Configure<MarketPipelineOptions>(options =>
        {
            options.EnableCaching = true;
            options.CacheExpirationSeconds = 300;
            options.ValidationRetries = 2;
            options.EnableEnrichment = true;
        });

        services.AddSingleton(new NormalizationOptions
        {
            MinimumWindowSize = 6,
            WinsorizeOutliers = true,
            WinsorizeZLimit = 4.0m
        });

        // Currency strength model
        services.AddSingleton<CurrencyStrengthModel>(_ =>
            new CurrencyStrengthModel(new List<CurrencyModel>
            {
                new() { Currency = "EUR", CarryWeight = 1m, GrowthWeight = 1m, InflationWeight = 1m, RiskWeight = 1m },
                new() { Currency = "USD", CarryWeight = 1m, GrowthWeight = 1m, InflationWeight = 1m, RiskWeight = 1m }
            }));

        return services;
    }

    private static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<MassiveClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Massive:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration["Massive:ApiKey"]}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<ForexEventScraperService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "ForexNewsService/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    private static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IMarketDataRepository, MarketDataRepository>();
        services.AddScoped<IMassiveForexRepository, MassiveForexRepository>();
        services.AddScoped<TradePlanFactory>();
        services.AddScoped<IAuditStorage, AuditStorage>();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IExecutionCostModel, SimpleExecutionCostModel>();
        services.AddScoped<ILatencyModel, SimpleLatencyModel>();
        services.AddScoped<ICorrelatedShockGenerator, CorrelatedShockGenerator>();
        services.AddScoped<IVolatilityService, VolatilityService>();
        services.AddScoped<INewsEventService, NewsEventService>();
        services.AddScoped<IMacroTransitionModel, MacroTransitionModel>();
        services.AddScoped<IOrderBookExecutionService, OrderBookExecutionService>();
        services.AddScoped<IIngestionValidator, IngestionValidator>();
        services.AddScoped<FxRelativePricer>();
        services.AddScoped<FxPriceSimulator>();
        services.AddScoped<IMarketDataEngine, MarketDataEngine>();
        services.AddScoped<IAlertEngine, AlertEngine>();
        services.AddScoped<IAlphaEngine, AlphaEngine>();
        services.AddScoped<IAuditTrailEngine, AuditTrailEngine>();

        // Repository and engine registrations
        services.AddScoped<IForexEventRepository, ForexEventRepository>();
        services.AddScoped<IForexNewsRepository, ForexNewsRepository>();
        services.AddScoped<INewsEngine, NewsEngine>();

        return services;
    }

    private static IServiceCollection AddEngineServices(this IServiceCollection services)
    {
        services.AddScoped<IAdvancedExecutionEngine, AdvancedExecutionEngine>();
        services.AddScoped<IBacktestEngine, BacktestEngine>();
        services.AddScoped<ICircuitBreakerEngine, CircuitBreakerEngine>();
        services.AddScoped<IComplianceEngine, ComplianceEngine>();
        services.AddScoped<ICorrelationEngine, CorrelationEngine>();
        services.AddScoped<IConfigurationEngine, ConfigurationEngine>();
        services.AddScoped<IDataQualityEngine, DataQualityEngine>();
        services.AddScoped<IEconomicCalendarRiskEngine, EconomicCalendarRiskEngine>();
        services.AddScoped<IExecutionEngine, ExecutionEngine>();
        services.AddScoped<IExitEngine, ExitEngine>();
        services.AddScoped<IFxPricingEngine, FxPricingEngine>();
        services.AddScoped<IHedgingEngine, HedgingEngine>();
        services.AddScoped<ILiquidityEngine, LiquidityEngine>();
        services.AddScoped<IMarketReplayEngine, MarketReplayEngine>();
        services.AddScoped<IModelValidationEngine, ModelValidationEngine>();
        services.AddScoped<IMonteCarloEngine, MonteCarloEngine>();
        services.AddScoped<INormalizationEngine, NormalizationEngine>();
        services.AddScoped<IOrderManagementEngine, OrderManagementEngine>();
        services.AddScoped<IPerformanceAnalyticsEngine, PerformanceAnalyticsEngine>();
        services.AddScoped<IPortfolioEngine, PortfolioEngine>();
        services.AddScoped<IPositionSizingEngine, PositionSizingEngine>();
        services.AddScoped<IProbabilisticScenarioEngine, ProbabilisticScenarioEngine>();
        services.AddScoped<IRealBacktestEngine, RealBacktestEngine>();
        services.AddScoped<IRealTimeRiskEngine, RealTimeRiskEngine>();
        services.AddScoped<IRiskEngine, RiskEngine>();
        services.AddScoped<IRegimeEngine, RegimeEngine>();
        services.AddScoped<IScenarioEngine, ScenarioEngine>();
        services.AddScoped<ISentimentEngine, SentimentEngine>();
        services.AddScoped<ISignalEngine, SignalEngine>();
        services.AddScoped<IMacroSimulationEngine, MacroSimulationEngine>();
        services.AddScoped<ILiveTradingOrchestrator, LiveTradingOrchestrator>();
        services.AddScoped<ITradeLifecycleEngine, TradeLifecycleEngine>();


        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddScoped<IOrderBookProvider, OrderBookProvider>();


        services.AddScoped<DailySmaPivotStrategy>();
        services.AddScoped<RiskManagementService>();

        // Add Quartz for scheduling
        services.AddQuartz(q =>
        {
            // Run daily strategy at 00:30 UTC (30 minutes after daily close)
            q.AddJob<DailyTradingStrategyJob>(opts => opts.WithIdentity("DailyTradingStrategyJob"));
            q.AddTrigger(opts => opts
                .ForJob("DailyTradingStrategyJob")
                .WithIdentity("DailyTradingStrategyTrigger")
                .WithCronSchedule("30 0 * * *")); // Runs at 00:30 UTC daily
        });
        services.AddQuartzHostedService();

        // Concrete implementations
        services.AddScoped<AdvancedExecutionEngine>();
        services.AddScoped<ConfigurationEngine>();
        services.AddScoped<ScenarioEngine>();
        services.AddScoped<ExitEngine>();

        return services;
    }

    private static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));

        return services;
    }

    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<MassiveBackgroundService>();
        services.AddHostedService<ForexDataBackgroundService>();

        return services;
    }

    private static IServiceCollection AddCorsServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AngularApp", policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static IServiceCollection AddRateLimitingServices(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
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

        return services;
    }

    private static IServiceCollection AddMartenDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMarten(options =>
        {
            options.Connection(configuration.GetConnectionString("OrionMacroDbConnection")!);
            options.AutoCreateSchemaObjects = AutoCreate.All;

            // Register document types
            options.Schema.For<MarketDataSnapshot>().Identity(x => x.Id);
            options.Schema.For<CandleDocument>().Identity(x => x.Id);
            options.Schema.For<TradePlan>().Identity(x => x.Id);
            options.Schema.For<MacroSnapshotDocument>().Identity(x => x.Id);
            options.Schema.For<EconomySeriesDocument>().Identity(x => x.Id);
            options.Schema.For<ForexSnapshotDocument>().Identity(x => x.Id);
            options.Schema.For<ForexTickerDocument>().Identity(x => x.Ticker);
            options.Schema.For<ForexQuoteDocument>().Identity(x => x.Id);
            options.Schema.For<ConversionDocument>().Identity(x => x.Id);
            options.Schema.For<IndicatorDocument>().Identity(x => x.Id);
            options.Schema.For<MarketStatusDocument>().Identity(x => x.Id);
            options.Schema.For<MarketHolidayDocument>().Identity(x => x.Id);

            // Analytics run documents — persist every backtest/Monte-Carlo/walk-forward/
            // performance/risk/compliance/calendar evaluation for audit and replay.
            options.Schema.For<BacktestRunDocument>().Identity(x => x.Id);
            options.Schema.For<WalkForwardRunDocument>().Identity(x => x.Id);
            options.Schema.For<MonteCarloRunDocument>().Identity(x => x.Id);
            options.Schema.For<PerformanceReportDocument>().Identity(x => x.Id);
            options.Schema.For<RealTimeRiskRunDocument>().Identity(x => x.Id);
            options.Schema.For<CircuitBreakerRunDocument>().Identity(x => x.Id);
            options.Schema.For<ComplianceRunDocument>().Identity(x => x.Id);
            options.Schema.For<EconomicCalendarRiskRunDocument>().Identity(x => x.Id);

            // Engine output documents — persist every decision/snapshot for replay & audit.
            options.Schema.For<RegimeRunDocument>().Identity(x => x.Id);
            options.Schema.For<ScenarioRunDocument>().Identity(x => x.Id);
            options.Schema.For<ProbabilisticScenarioRunDocument>().Identity(x => x.Id);
            options.Schema.For<MacroSimulationRunDocument>().Identity(x => x.Id);
            options.Schema.For<SignalRunDocument>().Identity(x => x.Id);
            options.Schema.For<RiskEvaluationRunDocument>().Identity(x => x.Id);
            options.Schema.For<CorrelationRunDocument>().Identity(x => x.Id);
            options.Schema.For<LiquidityRunDocument>().Identity(x => x.Id);
            options.Schema.For<HedgingRunDocument>().Identity(x => x.Id);
            options.Schema.For<SentimentRunDocument>().Identity(x => x.Id);
            options.Schema.For<ModelValidationRunDocument>().Identity(x => x.Id);
            options.Schema.For<PortfolioRiskRunDocument>().Identity(x => x.Id);
            options.Schema.For<LiveTradingRunDocument>().Identity(x => x.Id);
        });

        return services;
    }
}