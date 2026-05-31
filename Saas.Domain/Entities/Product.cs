namespace Saas.Domain.Entities;
using System.Text.Json.Serialization;
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public int TenantId { get; set; }
    [JsonIgnore] 
    public Tenant? Tenant { get; set; }
}