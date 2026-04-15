using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}