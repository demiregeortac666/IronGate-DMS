using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string StudentNo { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        
        [Required]
        public int RoomId { get; set; }

        
        public Room? Room { get; set; }

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}