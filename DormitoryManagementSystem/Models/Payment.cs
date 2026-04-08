using System;
using System.ComponentModel.DataAnnotations;

namespace DormitoryManagementSystem.Models
{
    public class Payment
    {
        public int Id { get; set; }

        // FK -> Invoice
        [Required]
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        [Range(0.01, 1000000)]
        public decimal Amount { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string? Method { get; set; } // Cash/Card/Transfer

        [StringLength(50)]
        public string? ReceiptNo { get; set; }
    }
}