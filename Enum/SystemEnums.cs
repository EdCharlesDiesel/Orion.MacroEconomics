namespace Orion.MacroEconomics.Enum;

public enum AuditRecordType
{
    Decision,
    PipelineStep,
    Error,
    Event,
    StateChange,
    Compliance
}

public enum ComplianceDecision
{
    Approved = 0,
    Rejected = 1,
    ManualReview = 2
}

public enum HealthComponentType
{
    DataProvider,
    PipelineEngine,
    ExternalService,
    Infrastructure,
    Database,
    Cache,
    MessageQueue,
    Custom
}

public enum RiskAction
{
    AllowTrade = 0,
    BlockTrade = 1,
    ReducePosition = 2,
    ClosePosition = 3,
    EmergencyFlatten = 4
}

public enum MarketRegime
{
    RiskOn,
    RiskOff,
    Stagflation,
    Goldilocks
}


public enum TradeDirection
{
    None  = 0,
    Long  = 1,
    Short = 2
}

public enum MarketRegimeFree
{
    Neutral = 0,
    RiskOn = 1,
    RiskOff = 2,
    InflationShock = 3,
    GrowthShock = 4,
    PolicyShock = 5
}
public enum TradePlanStatus
{
    Pending   = 0,
    Active    = 1,
    Closed    = 2,
    Cancelled = 3,
    StoppedOut = 4
}


public enum IndicatorFrequency
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}