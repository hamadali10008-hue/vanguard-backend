using System.Text.Json.Serialization;

namespace Saas.Domain.Entities;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Plan { get; set; } = "Free";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    [JsonIgnore] 
    public ICollection<Product> Products { get; set; } = new List<Product>();
}