using System.ComponentModel.DataAnnotations;

namespace Saas.Domain.Entities
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Completed, OnHold

        [Required]
        public int TenantId { get; set; } // The critical boundary isolation key

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
