namespace DormitoryManagementSystem.Models.Reports
{
    public class StudentBalanceVM
    {
        public string StudentName { get; set; } = null!;
        public string StudentNo { get; set; } = null!;
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
    }
}