using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        // FK -> Student (invoice owner)
        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        // Billing period, e.g. "2026-02"
        [Required]
        [StringLength(7)]
        public string PeriodMonth { get; set; } = "2026-01";

        // Base invoice amount
        [Range(0, 1000000)]
        public decimal Amount { get; set; }

        // Due date
        [Required]
        public DateTime DueDate { get; set; } = DateTime.Today;

        // Unpaid / Paid
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Unpaid";

        // --- PENALTY FIELDS ---

        [Range(0, 1000000)]
        public decimal PenaltyAmount { get; set; } = 0; // Late penalty amount

        public DateTime? PenaltyAppliedAt { get; set; } // When the penalty was applied

        // ----------------------------------

        // Partial payments recorded for this invoice
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}