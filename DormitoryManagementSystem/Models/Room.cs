using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]

        public string RoomNumber { get; set; } = string.Empty;

        [Range(1,20)]
        public int Capacity { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();

    }
}
