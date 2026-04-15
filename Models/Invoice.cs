using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        public Student Student { get; set; } = null!;

        [Required]
        [StringLength(7)]
        public string PeriodMonth { get; set; } = "2026-01";

        [Range(0, 1000000)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Unpaid";

        [Range(0, 1000000)]
        public decimal PenaltyAmount { get; set; } = 0;

        public DateTime? PenaltyAppliedAt { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}