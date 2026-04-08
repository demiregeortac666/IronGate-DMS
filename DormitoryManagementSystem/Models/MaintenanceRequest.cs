using System;
using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }

        // FK -> Room (room related to the request)
        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        // FK -> Student (student who created the request)
        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        // Request details, e.g. "Tap is leaking"
        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        // Status: Open, Approved, Closed
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ClosedAt { get; set; }

        // --- APPROVAL WORKFLOW FIELDS ---

        [StringLength(100)]
        public string? ApprovedBy { get; set; } // Approved by

        public DateTime? ApprovedAt { get; set; } // Approval date

        // -----------------------------------------------------
    }
}