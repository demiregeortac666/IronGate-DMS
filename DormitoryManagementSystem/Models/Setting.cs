using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class Setting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Value { get; set; } = string.Empty;
    }
}