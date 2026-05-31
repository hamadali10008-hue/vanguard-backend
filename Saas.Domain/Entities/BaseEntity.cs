namespace Saas.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    // Every single thing in our app will have a TenantId 
    // to keep data separate!
    public Guid TenantId { get; set; }
}