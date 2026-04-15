using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services; // AuditService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.Controllers
{
    // --- UPDATED AUTHORIZATION ---
    // Only Admin and Staff users can access invoice operations.
    [Authorize(Roles = "Admin,Staff")]
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly INotificationService _notify;

        public InvoicesController(AppDbContext context, AuditService audit, INotificationService notify)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
        }

        // INVOICE LIST: loads invoices and runs penalty checks in the background.
        public IActionResult Index(string? status)
        {
            // --- 1. AUTOMATIC LATE PENALTY LOGIC ---
            var penaltyRateSetting = _context.Settings.FirstOrDefault(s => s.Key == "LatePenaltyRate");
            var graceDaysSetting = _context.Settings.FirstOrDefault(s => s.Key == "PenaltyGraceDays");

            decimal penaltyRate = 0.05m;
            int graceDays = 7;

            if (penaltyRateSetting != null) decimal.TryParse(penaltyRateSetting.Value, out penaltyRate);
            if (graceDaysSetting != null) int.TryParse(graceDaysSetting.Value, out graceDays);

            var overdueInvoices = _context.Invoices
                .Where(i => i.Status == "Unpaid" &&
                            i.PenaltyAmount == 0 &&
                            i.DueDate.AddDays(graceDays) < DateTime.Now)
                .ToList();

            foreach (var inv in overdueInvoices)
            {
                inv.PenaltyAmount = Math.Round(inv.Amount * penaltyRate, 2);
                inv.PenaltyAppliedAt = DateTime.Now;
            }

            if (overdueInvoices.Any())
            {
                _context.SaveChanges();
                // Note: Bulk penalty actions can also be added to the audit log if needed.
            }

            // --- 2. LISTING AND FILTERING ---
            var query = _context.Invoices.Include(i => i.Student).AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            ViewBag.Status = status;
            return View(query.OrderByDescending(i => i.DueDate).ToList());
        }

        // CREATE INVOICE (GET)
        public IActionResult Create()
        {
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName");

            var defaultFeeSetting = _context.Settings.FirstOrDefault(s => s.Key == "DefaultMonthlyFee");
            decimal fee = 3500;
            if (defaultFeeSetting != null) decimal.TryParse(defaultFeeSetting.Value, out fee);

            var invoice = new Invoice { Amount = fee };
            return View(invoice);
        }

        // CREATE INVOICE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Invoice invoice)
        {
            if (_context.Invoices.Any(i => i.StudentId == invoice.StudentId && i.PeriodMonth == invoice.PeriodMonth))
            {
                ModelState.AddModelError("", "An invoice already exists for this student and period.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Invoices.Add(invoice);
                    _context.SaveChanges();

                    // LOG: Invoice created
                    _audit.Log("Create", "Invoice", invoice.Id, $"Created invoice for Period: {invoice.PeriodMonth}");

                    // AUTO-NOTIFY: Tell the student about the new invoice
                    _notify.SendToStudent(invoice.StudentId, $"📄 Yeni faturanız oluşturuldu! Dönem: {invoice.PeriodMonth}, Tutar: {invoice.Amount:C}");

                    TempData["Success"] = "Fatura başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Veritabanına kaydedilirken bir hata oluştu.");
                }
            }
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", invoice.StudentId);
            return View(invoice);
        }

        // EDIT INVOICE (GET)
        public IActionResult Edit(int id)
        {
            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == id);
            if (invoice == null) return NotFound();

            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", invoice.StudentId);
            return View(invoice);
        }

        // EDIT INVOICE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Invoice invoice)
        {
            if (_context.Invoices.Any(i => i.StudentId == invoice.StudentId && i.PeriodMonth == invoice.PeriodMonth && i.Id != invoice.Id))
            {
                ModelState.AddModelError("", "Aynı döneme ait başka bir fatura bu öğrenci için zaten mevcut.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Invoices.Update(invoice);
                    _context.SaveChanges();

                    // LOG: Invoice updated
                    _audit.Log("Update", "Invoice", invoice.Id, $"Updated invoice for Period: {invoice.PeriodMonth}");

                    TempData["Success"] = "Fatura başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Veritabanı güncellenirken bir hata oluştu.");
                }
            }
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", invoice.StudentId);
            return View(invoice);
        }

        // DELETE ACTION (GET)
        public IActionResult Delete(int id)
        {
            var invoice = _context.Invoices.Include(i => i.Student).FirstOrDefault(i => i.Id == id);
            if (invoice == null) return NotFound();
            return View(invoice);
        }

        // DELETE ACTION (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == id);
            if (invoice != null)
            {
                try
                {
                    string periodMonth = invoice.PeriodMonth;

                    _context.Invoices.Remove(invoice);
                    _context.SaveChanges();

                    // LOG: Invoice deleted
                    _audit.Log("Delete", "Invoice", id, $"Deleted invoice for Period: {periodMonth}");
                    TempData["Success"] = "Fatura sistemi üzerinden tamamen silindi.";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "Bu fatura silinemiyor çünkü bağlı ödemeler mevcut. Lütfen önce ödemeleri silin.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Student).ThenInclude(s => s.Room)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return NotFound();

            decimal totalPaid = invoice.Payments?.Sum(p => (decimal?)p.Amount) ?? 0;

            var vm = new InvoiceDetailsVM
            {
                Invoice = invoice,
                TotalPaid = totalPaid,
                RemainingAmount = (invoice.Amount + invoice.PenaltyAmount) - totalPaid,
                Payments = invoice.Payments?.OrderByDescending(p => p.PaidAt).ToList() ?? new List<Payment>()
            };

            return View(vm);
        }
    }
}