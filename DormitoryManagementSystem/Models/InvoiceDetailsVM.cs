using DormitoryManagementSystem.Models;
using System.Collections.Generic;

namespace DormitoryManagementSystem.Models
{
    public class InvoiceDetailsVM
    {
        public Invoice Invoice { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingAmount { get; set; }
        public List<Payment> Payments { get; set; }
    }
}