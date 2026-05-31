using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Saas.Domain.Entities;
using Saas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Saas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: api/auditlogs
        /// Retrieves the 15 most recent operational entries scoped strictly to the current tenant.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTenantAuditLogs()
        {
            // 🛡️ Multi-Tenant Gatekeeping: Extract the unique isolating tenant header token
            if (!Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdStr))
            {
                return BadRequest(new { error = "Missing isolation boundary parameter header (X-Tenant-Id)." });
            }

            string currentTenantId = tenantIdStr.ToString();

            try
            {
                // Fetch the audit entries ordered chronologically using our indexed parameters
                var logs = await _context.AuditLogs
                    .Where(log => log.TenantId == currentTenantId)
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(15)
                    .Select(log => new
                    {
                        log.Id,
                        log.UserName,
                        log.Action,
                        log.TargetType,
                        log.TargetName,
                        log.CreatedAt
                    })
                    .ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                // Log exception internally based on your logging architecture rules
                return StatusCode(500, new { error = "Failed to compile the active tenant audit trail data string stream.", details = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/auditlogs/internal-log
        /// Optional endpoint to manually write an audit trail record from internal service worker sub-routines.
        /// </summary>
        [HttpPost("internal-log")]
        public async Task<IActionResult> AppendAuditTrailRecord([FromBody] AuditLog logEntry)
        {
            if (!Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdStr))
            {
                return BadRequest(new { error = "Missing isolation boundary parameter header (X-Tenant-Id)." });
            }

            logEntry.TenantId = tenantIdStr.ToString();
            logEntry.CreatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _context.AuditLogs.Add(logEntry);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Audit trail instance compiled successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to commit mutation event log node.", details = ex.Message });
            }
        }
    }
}
