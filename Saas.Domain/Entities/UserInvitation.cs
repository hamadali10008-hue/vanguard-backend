namespace Saas.Domain.Entities
{
    public class UserInvitation
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = Guid.NewGuid().ToString();
        public string Role { get; set; } = "User"; // Default role for invited person
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(7);
        public bool IsUsed { get; set; } = false;

        // The Admin/Tenant who sent the invite
        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
