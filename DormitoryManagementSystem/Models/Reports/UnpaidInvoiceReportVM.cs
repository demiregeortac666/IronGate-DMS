using System;

namespace DormitoryManagementSystem.Models.Reports
{
    public class UnpaidInvoiceReportVM
    {
        public string StudentName { get; set; } = null!;
        public string StudentNo { get; set; } = null!;
        public string RoomNumber { get; set; } = null!;
        public string PeriodMonth { get; set; } = null!;
        public decimal InvoiceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime DueDate { get; set; }
    }
}