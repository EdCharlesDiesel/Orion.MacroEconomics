using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Entities;

public class AuditEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int SequenceNumber { get; set; }
    public AuditRecordType RecordType { get; set; }
    public Guid? CorrelationId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string? Pair { get; set; }
    public string Direction { get; set; } = string.Empty;
    public decimal? Confidence { get; set; }
    public object Data { get; set; } = new();
    public DateTime TimestampUtc { get; set; }
}