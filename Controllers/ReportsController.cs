using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Models.Reports;
using DormitoryManagementSystem.Services; // AuditService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.Controllers
{
    // --- UPDATED AUTHORIZATION ---
    // Financial reports and occupancy data are limited to Admin and Staff users.
    [Authorize(Roles = "Admin,Staff")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit; // Injected for audit logging

        public ReportsController(AppDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // 1. ODA DOLULUK RAPORU
        public IActionResult RoomOccupancy()
        {
            var report = _context.Rooms
                .Select(r => new RoomOccupancyVM
                {
                    RoomNumber = r.RoomNumber,
                    Capacity = r.Capacity,
                    StudentCount = _context.Students.Count(s => s.RoomId == r.Id)
                })
                .ToList();

            _audit.Log("View", "Report", null, "Viewed room occupancy report.");
            _context.SaveChanges();

            return View(report);
        }

        // 2. FINANCIAL SUMMARY
        public IActionResult FinanceSummary()
        {
            var vm = new FinanceSummaryVM
            {
                TotalInvoices = _context.Invoices.Sum(i => (decimal?)(i.Amount + i.PenaltyAmount)) ?? 0,
                TotalPaidPayments = _context.Payments.Sum(p => (decimal?)p.Amount) ?? 0,
                TotalUnpaidInvoices = _context.Invoices
                    .Where(i => i.Status == "Unpaid")
                    .Sum(i => (decimal?)(i.Amount + i.PenaltyAmount)) ?? 0,
                PaidInvoiceCount = _context.Invoices.Count(i => i.Status == "Paid"),
                UnpaidInvoiceCount = _context.Invoices.Count(i => i.Status == "Unpaid")
            };

            _audit.Log("View", "Report", null, "Viewed finance summary report.");
            _context.SaveChanges();

            return View(vm);
        }

        // 3. UNPAID INVOICES
        public IActionResult UnpaidInvoices()
        {
            var report = _context.Invoices
                .Include(i => i.Student)
                    .ThenInclude(s => s.Room)
                .Where(i => i.Status == "Unpaid")
                .Select(i => new UnpaidInvoiceReportVM
                {
                    StudentName = i.Student != null ? i.Student.FullName : "-",
                    StudentNo = i.Student != null ? i.Student.StudentNo : "-",
                    RoomNumber = i.Student != null && i.Student.Room != null ? i.Student.Room.RoomNumber : "-",
                    PeriodMonth = i.PeriodMonth,
                    InvoiceAmount = i.Amount + i.PenaltyAmount,
                    PaidAmount = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0,
                    RemainingAmount = (i.Amount + i.PenaltyAmount) - (i.Payments.Sum(p => (decimal?)p.Amount) ?? 0),
                    DueDate = i.DueDate
                })
                .OrderBy(r => r.DueDate)
                .ToList();

            _audit.Log("View", "Report", null, "Viewed unpaid invoices report.");
            _context.SaveChanges();

            return View(report);
        }

        // 4. MONTHLY COLLECTION REPORT
        public IActionResult MonthlyPayments()
        {
            var report = _context.Payments
                .AsNoTracking()
                .Select(p => new
                {
                    p.PaidAt,
                    p.Amount
                })
                .ToList()
                .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })
                .Select(g => new MonthlyPaymentsVM
                {
                    MonthYear = $"{g.Key.Year}-{g.Key.Month:D2}",
                    PaymentCount = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(r => r.MonthYear)
                .ToList();

            _audit.Log("View", "Report", null, "Viewed monthly payments report.");
            _context.SaveChanges();

            return View(report);
        }

        // 5. STUDENT BALANCE REPORT
        public IActionResult StudentBalance()
        {
            var report = _context.Students
                .AsNoTracking()
                .Include(s => s.Invoices)
                    .ThenInclude(i => i.Payments)
                .ToList()
                .Select(s => new StudentBalanceVM
                {
                    StudentName = s.FullName,
                    StudentNo = s.StudentNo,
                    TotalInvoiced = s.Invoices.Sum(i => i.Amount + i.PenaltyAmount),
                    TotalPaid = s.Invoices.SelectMany(i => i.Payments).Sum(p => p.Amount),
                    Balance = s.Invoices.Sum(i => i.Amount + i.PenaltyAmount) - s.Invoices.SelectMany(i => i.Payments).Sum(p => p.Amount)
                })
                .OrderByDescending(r => r.Balance)
                .ToList();

            _audit.Log("View", "Report", null, "Viewed student balance report.");
            _context.SaveChanges();

            return View(report);
        }
    }

    // Helper Model
    public class RoomOccupancyVM
    {
        public string RoomNumber { get; set; } = "";
        public int Capacity { get; set; }
        public int StudentCount { get; set; }
    }
}