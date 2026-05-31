using Microsoft.EntityFrameworkCore;
using Saas.Domain.Entities;
using static Saas.Domain.Entities.Task;


namespace Saas.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; set; }
    DbSet<Product> Products { get; set; }
    DbSet<User> Users { get; set; }

    DbSet<Project> Projects { get; set; }
    DbSet<AuditLog> AuditLogs{ get; set; }
    DbSet<TaskItem> Tasks  { get; set; }
    DbSet<UserInvitation> UserInvitations { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}