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
        [RegularExpression(@"^\d{4}-\d{2}$", ErrorMessage = "Period must be in YYYY-MM format (e.g. 2026-04).")]
        public string PeriodMonth { get; set; } = DormitoryManagementSystem.SystemTime.UtcNow.ToString("yyyy-MM");

        // Minimum 0.01 to prevent zero-value invoices that are instantly "Paid".
        [Range(0.01, 1_000_000, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; } = DormitoryManagementSystem.SystemTime.UtcNow.Date;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Unpaid";

        [Range(0, 1_000_000)]
        public decimal PenaltyAmount { get; set; } = 0;

        public DateTime? PenaltyAppliedAt { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
