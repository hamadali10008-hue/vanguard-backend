using Microsoft.AspNetCore.Mvc;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;

namespace Saas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _tenantService.GetAllTenantsAsync();
        return Ok(tenants);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Tenant name)
    {
        var tenant = await _tenantService.CreateTenantAsync(name);
        return Ok(tenant);
    }

    
}