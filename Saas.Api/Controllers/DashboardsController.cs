using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Saas.Infrastructure.Data;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Saas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummaryMetrics()
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();

            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            try
            {
                // Query all projects matching tenant boundary parameters
                var projectMetrics = await _context.Projects
                    .Where(p => p.TenantId == parsedTenantId)
                    .Select(p => p.Status)
                    .ToListAsync();

                // Query all tasks matching tenant boundary parameters
                var taskMetrics = await _context.Tasks
                    .Where(t => t.TenantId == parsedTenantId)
                    .Select(t => t.Status)
                    .ToListAsync();

                var summary = new DashboardSummaryDto
                {
                    TotalProjects = projectMetrics.Count,
                    ActiveProjects = projectMetrics.Count(s => string.Equals(s, "Active", StringComparison.OrdinalIgnoreCase)),
                    CompletedProjects = projectMetrics.Count(s => string.Equals(s, "Completed", StringComparison.OrdinalIgnoreCase)),

                    TotalTasks = taskMetrics.Count,
                    CompletedTasks = taskMetrics.Count(s => string.Equals(s, "Done", StringComparison.OrdinalIgnoreCase)),
                    PendingTasks = taskMetrics.Count(s => !string.Equals(s, "Done", StringComparison.OrdinalIgnoreCase))
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Compilation Fault: {ex.Message}");
            }
        }

    }
    public class DashboardSummaryDto
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
    }
}
