using Saas.Domain.Entities;

namespace Saas.Application.Interfaces;

public interface ITenantService 
{
    Task<IEnumerable<Tenant>> GetAllTenantsAsync();
    Task<Tenant> CreateTenantAsync(Tenant tenant);
}