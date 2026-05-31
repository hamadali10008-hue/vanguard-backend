using Saas.Application.Interfaces;

using Saas.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Saas.Appplication.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IApplicationDbContext _context;
        public AuditLogService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task SaveAuditLogAsync(string tenantId, int userId, string userName, string action, string targetType, string targetName)
        {
            var log = new AuditLog
            {
                TenantId = tenantId,
                UserId = userId,
                UserName = userName,
                Action = action,
                TargetType = targetType,
                TargetName = targetName,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }


        public async Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync()
        {
            return await _context.AuditLogs.ToListAsync();
        }

        public async Task<AuditLog> CreateAuditLogAsync(AuditLog auditLog)
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }

        public Task<AuditLog> SaveAuditLogAsync(AuditLog auditLog)
        {
            throw new NotImplementedException();
        }

        Task<AuditLog> IAuditLogService.SaveAuditLogAsync(string tenantId, int userId, string userName, string action, string targetType, string targetName)
        {
            throw new NotImplementedException();
        }
    }
}
