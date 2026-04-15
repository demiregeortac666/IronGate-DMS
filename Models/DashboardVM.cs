namespace DormitoryManagementSystem.Models
{
    public class DashboardVM
    {
        public int TotalRooms { get; set; }
        public int TotalStudents { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int OpenMaintenanceRequests { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalUnpaid { get; set; }

        // Chart Data
        public List<string> MonthlyPaymentsLabels { get; set; } = new();
        public List<decimal> MonthlyPaymentsData { get; set; } = new();
        public int PaidInvoicesCount { get; set; }
        public int UnpaidInvoicesCount { get; set; }
    }
}