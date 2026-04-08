namespace DormitoryManagementSystem.Models
{
    public class FinanceSummaryVM
    {
        public decimal TotalInvoices { get; set; }
        public decimal TotalPaidPayments { get; set; }
        public decimal TotalUnpaidInvoices { get; set; }
        public int PaidInvoiceCount { get; set; }
        public int UnpaidInvoiceCount { get; set; }
    }
}