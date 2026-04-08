using System.Diagnostics;
using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // [Authorize] iin bu ktphane art!

namespace DormitoryManagementSystem.Controllers
{
    [Authorize] // <--- Yurdun "Dashboard" (zet) ekranna da kilit vuruldu!
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DASHBOARD: Yurdun genel durumunu zetler
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Index()
        {
            var vm = new DashboardVM
            {
                TotalRooms = _context.Rooms.Count(),
                TotalStudents = _context.Students.Count(),
                TotalBeds = _context.Rooms.Sum(r => (int?)r.Capacity) ?? 0,
                OccupiedBeds = _context.Students.Count(),
                TotalPaid = _context.Payments.Sum(p => (decimal?)p.Amount) ?? 0,
                TotalUnpaid = _context.Invoices
                    .Where(i => i.Status == "Unpaid")
                    .Sum(i => (decimal?)(i.Amount + i.PenaltyAmount)) ?? 0,
                OpenMaintenanceRequests = _context.MaintenanceRequests.Count(m => m.Status == "Open")
            };

            return View(vm);
        }

        // GZLLK POLTKASI
        public IActionResult Privacy()
        {
            return View();
        }

        // HATA SAYFASI
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}