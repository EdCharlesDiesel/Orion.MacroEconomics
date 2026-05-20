using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

public interface IAuditStorage
{
    Task StoreBatchAsync(List<AuditEntry> entries);
    Task<AuditQueryResult> QueryAsync(AuditQuery query);
    Task<AuditEntry> GetByIdAsync(Guid id);
    Task ArchiveAsync(DateTime before);
}