using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;
using static Saas.Domain.Entities.Task;


namespace Saas.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Product> Products { get; set; }

    public DbSet<User> Users{ get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<Project> Projects { get; set; }

    public DbSet<AuditLog> AuditLogs{ get; set; }
    public DbSet<UserInvitation> UserInvitations { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Fixes the Price warning
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        // Fixes the TenantId1 error by explicitly mapping the relationship
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Tenant)
            .WithMany(t => t.Products)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade); // If a tenant is deleted, delete their products
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        // 1. Define the private field for the database
        private readonly ApplicationDbContext _context;

        // 2. Inject it into the Constructor
        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- YOUR ENDPOINTS (like register-admin) GO BELOW HERE ---
    }
}