namespace DormitoryManagementSystem.Models.Reports
{
    public class MonthlyPaymentsVM
    {
        public string MonthYear { get; set; } = null!;
        public int PaymentCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}