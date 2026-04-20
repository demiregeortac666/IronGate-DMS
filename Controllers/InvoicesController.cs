using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DormitoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly INotificationService _notify;

        public InvoicesController(AppDbContext context, AuditService audit, INotificationService notify)
        {
            _context = context;
            _audit   = audit;
            _notify  = notify;
        }

        // INVOICE LIST
        public IActionResult Index(string? status)
        {
            var query = _context.Invoices.Include(i => i.Student).AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(i => i.Status == status);

            ViewBag.Status = status;
            return View(query.OrderByDescending(i => i.DueDate).ToList());
        }

        // APPLY LATE PENALTIES (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyLatePenalties(string? status)
        {
            var (penaltyRate, graceDays) = GetPenaltySettings();
            var now = DormitoryManagementSystem.SystemTime.UtcNow;

            var overdueInvoices = _context.Invoices
                .Where(i => i.Status == "Unpaid" &&
                            i.PenaltyAmount == 0 &&
                            i.DueDate.AddDays(graceDays) < now)
                .ToList();

            foreach (var inv in overdueInvoices)
            {
                inv.PenaltyAmount    = Math.Round(inv.Amount * penaltyRate, 2);
                inv.PenaltyAppliedAt = now;
            }

            if (overdueInvoices.Any())
            {
                _audit.Log("Update", "Invoice", null, $"Applied late penalties to {overdueInvoices.Count} invoices.");
                _context.SaveChanges();
                TempData["Success"] = $"Late penalties applied to {overdueInvoices.Count} invoice(s).";
            }
            else
            {
                TempData["Success"] = "No overdue unpaid invoices were found for penalty.";
            }

            return RedirectToAction(nameof(Index), new { status });
        }

        // CREATE INVOICE (GET)
        public IActionResult Create()
        {
            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName");

            var defaultFeeSetting = _context.Settings.FirstOrDefault(s => s.Key == "DefaultMonthlyFee");
            decimal fee = 3500;
            if (defaultFeeSetting != null &&
                decimal.TryParse(defaultFeeSetting.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedFee) &&
                parsedFee > 0)
            {
                fee = parsedFee;
            }

            return View(new Invoice { Amount = fee });
        }

        // CREATE INVOICE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Invoice invoice)
        {
            ModelState.Remove("Student");
            
            if (_context.Invoices.Any(i => i.StudentId == invoice.StudentId && i.PeriodMonth == invoice.PeriodMonth))
                ModelState.AddModelError("", "An invoice already exists for this student and period.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Invoices.Add(invoice);
                    _audit.Log("Create", "Invoice", invoice.Id, $"Created invoice for Period: {invoice.PeriodMonth}");
                    _context.SaveChanges();

                    _notify.SendToStudent(invoice.StudentId,
                        $"Your new invoice has been created. Period: {invoice.PeriodMonth}, Amount: {invoice.Amount:C}");

                    TempData["Success"] = "Invoice successfully created.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "An error occurred while saving to the database.");
                }
            }

            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", invoice.StudentId);
            return View(invoice);
        }

        // EDIT INVOICE (GET)
        public IActionResult Edit(int id)
        {
            var invoice = _context.Invoices.Include(i => i.Student).FirstOrDefault(i => i.Id == id);
            if (invoice == null) return NotFound();

            // StudentId is read-only during edit — shown as text, not a dropdown.
            return View(invoice);
        }

        // EDIT INVOICE (POST)
        // Only PeriodMonth, Amount, and DueDate may be changed.
        // Status is derived by SyncInvoiceStatus; PenaltyAmount is managed by ApplyLatePenalties.
        // StudentId cannot be reassigned after creation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string periodMonth, decimal amount, DateTime dueDate)
        {
            var existing = _context.Invoices.Include(i => i.Student).FirstOrDefault(i => i.Id == id);
            if (existing == null) return NotFound();

            // Validate PeriodMonth format manually since it comes as a plain parameter
            if (string.IsNullOrWhiteSpace(periodMonth) ||
                !System.Text.RegularExpressions.Regex.IsMatch(periodMonth, @"^\d{4}-\d{2}$"))
            {
                ModelState.AddModelError("", "Period must be in YYYY-MM format (e.g. 2026-04).");
            }

            if (amount < 0.01m)
                ModelState.AddModelError("", "Amount must be greater than zero.");

            if (_context.Invoices.Any(i => i.StudentId == existing.StudentId &&
                                           i.PeriodMonth == periodMonth &&
                                           i.Id != id))
            {
                ModelState.AddModelError("", "Another invoice for this student and period already exists.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", existing.StudentId);
                return View(existing);
            }

            try
            {
                existing.PeriodMonth = periodMonth;
                existing.Amount      = amount;
                existing.DueDate     = dueDate;

                _audit.Log("Update", "Invoice", existing.Id, $"Updated invoice for Period: {existing.PeriodMonth}");
                _context.SaveChanges();

                // Re-derive Status in case the amount change altered the paid/unpaid balance.
                SyncInvoiceStatus(existing.Id);

                TempData["Success"] = "Invoice successfully updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "An error occurred while updating the database.");
            }

            ViewBag.StudentId = new SelectList(_context.Students.OrderBy(s => s.FullName), "Id", "FullName", existing.StudentId);
            return View(existing);
        }

        // DELETE (GET)
        public IActionResult Delete(int id)
        {
            var invoice = _context.Invoices.Include(i => i.Student).FirstOrDefault(i => i.Id == id);
            if (invoice == null) return NotFound();
            return View(invoice);
        }

        // DELETE (POST)
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
                    _audit.Log("Delete", "Invoice", id, $"Deleted invoice for Period: {periodMonth}");
                    _context.SaveChanges();
                    TempData["Success"] = "Invoice completely deleted from the system.";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "This invoice cannot be deleted because there are linked payments. Please delete the payments first.";
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
                Invoice         = invoice,
                TotalPaid       = totalPaid,
                RemainingAmount = (invoice.Amount + invoice.PenaltyAmount) - totalPaid,
                Payments        = invoice.Payments?.OrderByDescending(p => p.PaidAt).ToList() ?? new List<Payment>()
            };

            return View(vm);
        }

        // Derives Status from actual payment totals and persists it.
        internal void SyncInvoiceStatus(int invoiceId)
        {
            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice == null) return;

            var paidSum   = _context.Payments.Where(p => p.InvoiceId == invoiceId).Sum(p => (decimal?)p.Amount) ?? 0;
            var totalOwed = invoice.Amount + invoice.PenaltyAmount;

            invoice.Status = paidSum >= totalOwed ? "Paid" : "Unpaid";
            _context.SaveChanges();
        }

        private (decimal PenaltyRate, int GraceDays) GetPenaltySettings()
        {
            decimal penaltyRate = 0.05m;
            int graceDays = 7;

            var penaltyRateSetting = _context.Settings.AsNoTracking().FirstOrDefault(s => s.Key == "LatePenaltyRate");
            var graceDaysSetting   = _context.Settings.AsNoTracking().FirstOrDefault(s => s.Key == "PenaltyGraceDays");

            if (penaltyRateSetting != null &&
                decimal.TryParse(penaltyRateSetting.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedRate) &&
                parsedRate >= 0)
            {
                penaltyRate = parsedRate;
            }

            if (graceDaysSetting != null &&
                int.TryParse(graceDaysSetting.Value, out var parsedDays) &&
                parsedDays >= 0)
            {
                graceDays = parsedDays;
            }

            return (penaltyRate, graceDays);
        }
    }
}
