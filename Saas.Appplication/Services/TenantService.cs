using Microsoft.EntityFrameworkCore;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;


namespace Saas.Application.Services;

public class TenantService : ITenantService
{
    private readonly IApplicationDbContext _context;

    public TenantService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
    {
        return await _context.Tenants.ToListAsync();
    }

    public async Task<Tenant> CreateTenantAsync(Tenant tenant)
    {
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }
}