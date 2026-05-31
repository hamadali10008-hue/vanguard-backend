using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Saas.Domain.Entities
{
    public class Task
    {
        [Table("Tasks")]
        public class TaskItem
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [StringLength(150)]
            public string Title { get; set; } = string.Empty;

            [Required]
            [StringLength(20)]
            public string Status { get; set; } = "Todo"; // Todo, InProgress, Done

            [Required]
            public int ProjectId { get; set; } // Foreign Key linking to the Project container

            [Required]
            public int TenantId { get; set; } // Enforces strict SaaS separation boundaries

            // 🔗 NEW: Optional foreign key mapping to the assigned user's record ID
            public int? AssignedUserId { get; set; }

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}
