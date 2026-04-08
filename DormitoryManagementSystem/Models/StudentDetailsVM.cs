using DormitoryManagementSystem.Models; // Adjust if your namespace changes
using System.Collections.Generic;

namespace DormitoryManagementSystem.Models
{
    public class StudentDetailsVM
    {
        public Student Student { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingDebt { get; set; }
        public List<Invoice> Invoices { get; set; }
    }
}