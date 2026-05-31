using System.Text.Json.Serialization;

namespace Saas.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TenantId { get; set; }
        [JsonIgnore]
        public Tenant? Tenant { get; set; }
    }
}
