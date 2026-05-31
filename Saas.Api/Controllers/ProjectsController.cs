using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saas.Infrastructure.Data;
using Saas.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Saas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🟢 GET: api/Projects - Fetches isolated tenant projects WITH nested tasks and assigned users
        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            try
            {
                var projectsData = await _context.Projects
                    .Where(p => p.TenantId == parsedTenantId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Status,
                        p.TenantId,
                        p.CreatedAt,
                        Tasks = _context.Tasks
                            .Where(t => t.ProjectId == p.Id && t.TenantId == parsedTenantId)
                            .Select(t => new
                            {
                                t.Id,
                                t.Title,
                                t.Status,
                                t.AssignedUserId,
                                AssignedUserName = _context.Users
                                    .Where(u => u.Id == t.AssignedUserId && u.TenantId == parsedTenantId)
                                    .Select(u => u.FullName)
                                    .FirstOrDefault() ?? "Unassigned"
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(projectsData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Query Error: {ex.Message}");
            }
        }

        // 🔵 POST: api/Projects - Creates a project tied directly to the tenant context
        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto request)
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            var userRoleHeader = Request.Headers["X-User-Role"].ToString().Trim();

            if (!string.Equals(userRoleHeader, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, "Access Denied: Only workspace administrators can initialize projects.");
            }
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("Project naming parameters cannot be left blank.");
            }

            try
            {
                var newProject = new Project
                {
                    Name = request.Name.Trim(),
                    Description = request.Description.Trim(),
                    Status = "Active",
                    TenantId = parsedTenantId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Projects.Add(newProject);
                await _context.SaveChangesAsync();

                // 📈 DYNAMIC AUDIT LOG: Extract incoming user name header strings
                string currentUserName = Request.Headers["X-User-Name"].ToString().Trim();
                if (string.IsNullOrEmpty(currentUserName))
                {
                    currentUserName = "Workspace Admin"; // Reliable fallback signature
                }

                await SaveAuditLogAsync(tenantIdHeader, 0, currentUserName, "CREATED", "PROJECT", newProject.Name);

                return Ok(newProject);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Write Error: {ex.Message}");
            }
        }

        // 🟡 PUT: api/Projects/{id} - Update project details or status
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectDto request)
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            try
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == parsedTenantId);
                if (project == null)
                {
                    return NotFound("Target project node not found or access is restricted.");
                }

                if (!string.IsNullOrEmpty(request.Status) &&
                    request.Status != "Active" && request.Status != "Completed" && request.Status != "OnHold")
                {
                    return BadRequest("Invalid lifecycle status node specified.");
                }

                bool isStatusChanging = !string.IsNullOrEmpty(request.Status) && project.Status != request.Status;

                if (!string.IsNullOrEmpty(request.Name)) project.Name = request.Name.Trim();
                if (request.Description != null) project.Description = request.Description.Trim();
                if (!string.IsNullOrEmpty(request.Status)) project.Status = request.Status;

                await _context.SaveChangesAsync();

                // 📈 DYNAMIC AUDIT LOG: Extract incoming user name header strings
                string currentUserName = Request.Headers["X-User-Name"].ToString().Trim();
                if (string.IsNullOrEmpty(currentUserName))
                {
                    currentUserName = "Team Member"; // Fallback identifier
                }

                string actionName = isStatusChanging ? "TOGGLED_STATUS" : "UPDATED";
                await SaveAuditLogAsync(tenantIdHeader, 0, currentUserName, actionName, "PROJECT", project.Name);

                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Update Error: {ex.Message}");
            }
        }

        // 🔴 DELETE: api/Projects/{id} - Purge project from database ledger
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            var userRoleHeader = Request.Headers["X-User-Role"].ToString().Trim();

            if (!string.Equals(userRoleHeader, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, "Access Denied: Only workspace administrators can delete projects.");
            }
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            try
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == parsedTenantId);
                if (project == null)
                {
                    return NotFound("Target project node not found or access is restricted.");
                }

                string deletedProjectName = project.Name;

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();

                // 📈 DYNAMIC AUDIT LOG: Extract incoming user name header strings
                string currentUserName = Request.Headers["X-User-Name"].ToString().Trim();
                if (string.IsNullOrEmpty(currentUserName))
                {
                    currentUserName = "Workspace Admin";
                }

                await SaveAuditLogAsync(tenantIdHeader, 0, currentUserName, "DELETED", "PROJECT", deletedProjectName);

                return Ok(new { message = "Project infrastructure cleanly purged from secure ledger entries." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Deletion Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Reusable sub-routine handler to write operational history straight to the system ledger.
        /// </summary>
        private async System.Threading.Tasks.Task SaveAuditLogAsync(string tenantId, int userId, string userName, string action, string targetType, string targetName)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audit logging subsystem failure exception: {ex.Message}");
            }
        }
    }

    public class CreateProjectDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateProjectDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
    }
}