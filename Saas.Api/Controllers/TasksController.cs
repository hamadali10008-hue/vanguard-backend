using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saas.Domain.Entities;
using Saas.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Saas.Domain.Entities.Task;

namespace Saas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🟢 GET: api/Tasks/project/{projectId} - Get tasks for a specific project
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetProjectTasks(int projectId)
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            try
            {
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId && p.TenantId == parsedTenantId);
                if (!projectExists)
                {
                    return NotFound("Parent project node not found or access is restricted.");
                }

                var tasks = await _context.Tasks
                    .Where(t => t.ProjectId == projectId && t.TenantId == parsedTenantId)
                    .OrderBy(t => t.Id)
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Query Error: {ex.Message}");
            }
        }

        // 🟢 POST: api/Tasks - Create a new task
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto request)
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            var userRoleHeader = Request.Headers["X-User-Role"].ToString().Trim();

            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            if (!string.Equals(userRoleHeader, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, "Access Denied: Only platform administrators can append task objectives.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Task title metrics cannot be blank.");
            }

            try
            {
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId && p.TenantId == parsedTenantId);
                if (!projectExists)
                {
                    return BadRequest("Invalid project contextual target.");
                }

                var newTask = new TaskItem
                {
                    Title = request.Title.Trim(),
                    Status = "Todo",
                    ProjectId = request.ProjectId,
                    TenantId = parsedTenantId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tasks.Add(newTask);
                await _context.SaveChangesAsync();

                // 📈 AUDIT LOG: Record task creation
                await SaveAuditLogAsync(tenantIdHeader, 0, "Workspace Admin", "CREATED", "TASK", newTask.Title);

                return Ok(newTask);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Write Error: {ex.Message}");
            }
        }

        // 🟡 PATCH: api/Tasks/{id}/status - Rapidly move task between Todo, InProgress, Done
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusDto request)
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            if (request.Status != "Todo" && request.Status != "InProgress" && request.Status != "Done")
            {
                return BadRequest("Invalid task pipeline lane.");
            }

            try
            {
                var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == parsedTenantId);
                if (task == null)
                {
                    return NotFound("Target task row not found or access restricted.");
                }

                task.Status = request.Status;
                await _context.SaveChangesAsync();

                // 📈 AUDIT LOG: Record task completion checkbox changes
                await SaveAuditLogAsync(tenantIdHeader, 0, "Workspace Admin", "TOGGLED_STATUS", "TASK", task.Title);

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Mutation Error: {ex.Message}");
            }
        }

        // 🔵 PATCH: api/Tasks/{id}/assign - Assign a task to a user within the tenant
        [HttpPatch("{id}/assign")]
        public async Task<IActionResult> AssignTask(int id, [FromBody] AssignTaskDto request)
        {
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                return BadRequest("Missing or invalid multi-tenant contextual tracking header.");
            }

            try
            {
                var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == parsedTenantId);
                if (task == null)
                {
                    return NotFound("Target task row not found or access restricted.");
                }

                task.AssignedUserId = request.AssignedUserId;
                await _context.SaveChangesAsync();

                // 🔍 Fetch assigned user's full name to create a contextual description for the ledger
                string assignedName = "Unassigned";
                if (request.AssignedUserId.HasValue)
                {
                    assignedName = await _context.Users
                        .Where(u => u.Id == request.AssignedUserId.Value && u.TenantId == parsedTenantId)
                        .Select(u => u.FullName)
                        .FirstOrDefaultAsync() ?? "User Context";
                }

                // 📈 AUDIT LOG: Record task ownership adjustment 
                await SaveAuditLogAsync(tenantIdHeader, 0, "Workspace Admin", "ASSIGNED_USER", "TASK", $"{task.Title} (Assigned to: {assignedName})");

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Assignment Error: {ex.Message}");
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
                // Soft catch pipeline errors to prevent blocking primary mutations
                System.Diagnostics.Debug.WriteLine($"Audit logging subsystem failure exception: {ex.Message}");
            }
        }
    }

    public class AssignTaskDto
    {
        public int? AssignedUserId { get; set; }
    }

    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public int ProjectId { get; set; }
    }

    public class UpdateTaskStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}