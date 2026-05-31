using Saas.Domain.Entities;

namespace Saas.Application.Interfaces
{
    public interface IAuditLogService
    {
        Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync();
        Task<AuditLog> CreateAuditLogAsync(AuditLog auditLog);

        Task<AuditLog> SaveAuditLogAsync(string tenantId, int userId, string userName, string action, string targetType, string targetName);
    }
}
