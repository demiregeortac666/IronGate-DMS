using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services; // AuditService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.Controllers
{
    // Only Admin and Staff users can create or delete payment records.
    [Authorize(Roles = "Admin,Staff")]
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly INotificationService _notify;

        public PaymentsController(AppDbContext context, AuditService audit, INotificationService notify)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
        }

        // PAYMENT LIST: shows who paid, when, and how much
        public IActionResult Index()
        {
            var payments = _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Student)
                .OrderByDescending(p => p.PaidAt)
                .ToList();

            return View(payments);
        }

        // CREATE PAYMENT (GET)
        public IActionResult Create(int? invoiceId)
        {
            ViewBag.InvoiceId = GetInvoiceSelectList(invoiceId);

            if (invoiceId.HasValue)
                return View(new Payment { InvoiceId = invoiceId.Value });

            return View();
        }

        // CREATE PAYMENT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Payment payment)
        {
            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == payment.InvoiceId);
            if (invoice != null)
            {
                var totalPaid = _context.Payments.Where(p => p.InvoiceId == invoice.Id).Sum(p => (decimal?)p.Amount) ?? 0;
                var totalOwed = invoice.Amount + invoice.PenaltyAmount;
                var remaining = totalOwed - totalPaid;

                if (payment.Amount > remaining)
                {
                    ModelState.AddModelError("Amount", $"Payment amount cannot exceed the remaining balance of {remaining:C}.");
                }
            }
            else
            {
                ModelState.AddModelError("InvoiceId", "Invalid invoice.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Payments.Add(payment);
                    _context.SaveChanges();

                    // LOG: Payment created
                    _audit.Log("Create", "Payment", payment.Id, $"Created payment of {payment.Amount} for Invoice ID: {payment.InvoiceId}");

                    // AUTO-NOTIFY: Confirm the payment to the student
                    if (invoice != null)
                    {
                        _notify.SendToStudent(invoice.StudentId, $"✅ Ödemeniz alındı! Tutar: {payment.Amount:C}, Fatura ID: {payment.InvoiceId}");
                    }

                    SyncInvoiceStatus(payment.InvoiceId);
                    TempData["Success"] = "Ödeme başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Veritabanına kaydedilirken bir hata oluştu.");
                }
            }

            ViewBag.InvoiceId = GetInvoiceSelectList(payment.InvoiceId);
            return View(payment);
        }

        // EDIT PAYMENT (GET)
        public IActionResult Edit(int id)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null) return NotFound();

            ViewBag.InvoiceId = GetInvoiceSelectList(payment.InvoiceId);
            return View(payment);
        }

        // EDIT PAYMENT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Payment payment)
        {
            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == payment.InvoiceId);
            if (invoice != null)
            {
                var otherPaymentsTotal = _context.Payments.Where(p => p.InvoiceId == invoice.Id && p.Id != payment.Id).Sum(p => (decimal?)p.Amount) ?? 0;
                var totalOwed = invoice.Amount + invoice.PenaltyAmount;
                
                if (otherPaymentsTotal + payment.Amount > totalOwed)
                {
                    ModelState.AddModelError("Amount", $"Payment amount cannot exceed the remaining balance of {(totalOwed - otherPaymentsTotal):C}.");
                }
            }
            else
            {
                ModelState.AddModelError("InvoiceId", "Invalid invoice.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Payments.Update(payment);
                    _context.SaveChanges();

                    // LOG: Payment updated
                    _audit.Log("Update", "Payment", payment.Id, $"Updated payment ID: {payment.Id} (New Amount: {payment.Amount})");

                    SyncInvoiceStatus(payment.InvoiceId);
                    TempData["Success"] = "Ödeme başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Veritabanı güncellenirken bir hata oluştu.");
                }
            }

            ViewBag.InvoiceId = GetInvoiceSelectList(payment.InvoiceId);
            return View(payment);
        }

        // DELETE CONFIRMATION
        public IActionResult Delete(int id)
        {
            var payment = _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Student)
                .FirstOrDefault(p => p.Id == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        // DELETE ACTION
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null) return NotFound();

            var invoiceId = payment.InvoiceId;
            var amount = payment.Amount;

            try
            {
                _context.Payments.Remove(payment);
                _context.SaveChanges();

                // LOG: Payment deleted
                _audit.Log("Delete", "Payment", id, $"Deleted payment of {amount} for Invoice ID: {invoiceId}");

                SyncInvoiceStatus(invoiceId);
                TempData["Success"] = "Ödeme başarıyla silindi.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Bu ödeme silinirken bir veritabanı hatası oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }

        // YARDIMCI METOTLAR
        private SelectList GetInvoiceSelectList(int? selectedInvoiceId = null)
        {
            var invoiceList = _context.Invoices
                .Include(i => i.Student)
                .OrderByDescending(i => i.DueDate)
                .Select(i => new
                {
                    i.Id,
                    Text = $"{i.Id} - {(i.Student != null ? i.Student.FullName : "-")} - {i.PeriodMonth} - {i.Amount} - {i.Status}"
                })
                .ToList();

            return new SelectList(invoiceList, "Id", "Text", selectedInvoiceId);
        }

        // --- UPDATED METHOD (Bug Fix) ---
        private void SyncInvoiceStatus(int invoiceId)
        {
            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice == null) return;

            var paidSum = _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .Sum(p => (decimal?)p.Amount) ?? 0;

            // Total debt calculation including penalty
            var totalOwed = invoice.Amount + invoice.PenaltyAmount;

            invoice.Status = paidSum >= totalOwed ? "Paid" : "Unpaid";
            _context.SaveChanges();
        }
    }
}