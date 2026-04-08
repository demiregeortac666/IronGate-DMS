using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Details { get; set; }
    }
}