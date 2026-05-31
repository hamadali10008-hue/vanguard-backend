using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Saas.Domain.Entities
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TenantId { get; set; } = string.Empty; // 🔑 Crucial multi-tenant isolation key

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty; // Snapshotted name to preserve speed and historical accuracy

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // "CREATED", "TOGGLED_STATUS", "ASSIGNED_USER", "DELETED"

        [Required]
        [MaxLength(50)]
        public string TargetType { get; set; } = string.Empty; // "PROJECT", "TASK"

        [Required]
        [MaxLength(255)]
        public string TargetName { get; set; } = string.Empty; // e.g., "Q2 Security Ledger Audits"

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
