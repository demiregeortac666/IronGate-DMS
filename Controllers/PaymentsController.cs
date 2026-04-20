using DormitoryManagementSystem.Data;
using DormitoryManagementSystem.Models;
using DormitoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly INotificationService _notify;
        private readonly InvoicesController _invoiceCtrl;

        public PaymentsController(AppDbContext context, AuditService audit, INotificationService notify)
        {
            _context     = context;
            _audit       = audit;
            _notify      = notify;
            _invoiceCtrl = new InvoicesController(context, audit, notify);
        }

        // PAYMENT LIST
        public IActionResult Index()
        {
            var payments = _context.Payments
                .Include(p => p.Invoice).ThenInclude(i => i.Student)
                .OrderByDescending(p => p.PaidAt)
                .ToList();

            return View(payments);
        }

        // CREATE (GET)
        public IActionResult Create(int? invoiceId)
        {
            ViewBag.InvoiceId = GetInvoiceSelectList(invoiceId);

            if (invoiceId.HasValue)
                return View(new Payment { InvoiceId = invoiceId.Value });

            return View();
        }

        // CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Payment payment)
        {
            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == payment.InvoiceId);

            if (invoice == null)
            {
                ModelState.AddModelError("InvoiceId", "Invalid invoice.");
            }
            else
            {
                if (payment.Amount <= 0)
                    ModelState.AddModelError("Amount", "Payment amount must be greater than zero.");

                var totalPaid  = _context.Payments.Where(p => p.InvoiceId == invoice.Id).Sum(p => (decimal?)p.Amount) ?? 0;
                var totalOwed  = invoice.Amount + invoice.PenaltyAmount;
                var remaining  = totalOwed - totalPaid;

                if (payment.Amount > remaining)
                    ModelState.AddModelError("Amount", $"Payment amount cannot exceed the remaining balance of {remaining:C}.");
            }

            ModelState.Remove("Invoice");

            if (ModelState.IsValid)
            {
                // Wrap payment insert + status sync in a single transaction to prevent
                // concurrent payments producing inconsistent invoice status.
                using var tx = _context.Database.BeginTransaction();
                try
                {
                    _context.Payments.Add(payment);
                    _audit.Log("Create", "Payment", payment.Id,
                        $"Created payment of {payment.Amount} for Invoice ID: {payment.InvoiceId}");
                    _context.SaveChanges();

                    if (invoice != null)
                        _notify.SendToStudent(invoice.StudentId,
                            $"Your payment has been received. Amount: {payment.Amount:C}, Invoice ID: {payment.InvoiceId}");

                    _invoiceCtrl.SyncInvoiceStatus(payment.InvoiceId);

                    tx.Commit();
                    TempData["Success"] = "Payment successfully created.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    tx.Rollback();
                    ModelState.AddModelError("", "An error occurred while saving to the database.");
                }
            }

            ViewBag.InvoiceId = GetInvoiceSelectList(payment.InvoiceId);
            return View(payment);
        }

        // EDIT (GET)
        public IActionResult Edit(int id)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null) return NotFound();

            ViewBag.InvoiceId = GetInvoiceSelectList(payment.InvoiceId);
            return View(payment);
        }

        // EDIT (POST)
        // Only Amount and Method are editable; InvoiceId and PaidAt are immutable after creation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, decimal amount, string? method, string? receiptNo)
        {
            var existing = _context.Payments.FirstOrDefault(p => p.Id == id);
            if (existing == null) return NotFound();

            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == existing.InvoiceId);
            if (invoice == null)
            {
                ModelState.AddModelError("", "Associated invoice not found.");
            }
            else
            {
                if (amount <= 0)
                    ModelState.AddModelError("", "Amount must be greater than zero.");

                var otherPaid  = _context.Payments
                    .Where(p => p.InvoiceId == existing.InvoiceId && p.Id != id)
                    .Sum(p => (decimal?)p.Amount) ?? 0;
                var totalOwed  = invoice.Amount + invoice.PenaltyAmount;

                if (otherPaid + amount > totalOwed)
                    ModelState.AddModelError("",
                        $"Payment amount cannot exceed the remaining balance of {(totalOwed - otherPaid):C}.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.InvoiceId = GetInvoiceSelectList(existing.InvoiceId);
                return View(existing);
            }

            using var tx = _context.Database.BeginTransaction();
            try
            {
                existing.Amount    = amount;
                existing.Method    = method;
                existing.ReceiptNo = receiptNo;

                _audit.Log("Update", "Payment", existing.Id,
                    $"Updated payment ID: {existing.Id} (New Amount: {existing.Amount})");
                _context.SaveChanges();

                _invoiceCtrl.SyncInvoiceStatus(existing.InvoiceId);

                tx.Commit();
                TempData["Success"] = "Payment successfully updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                tx.Rollback();
                ModelState.AddModelError("", "An error occurred while updating the database.");
            }

            ViewBag.InvoiceId = GetInvoiceSelectList(existing.InvoiceId);
            return View(existing);
        }

        // DELETE (GET)
        public IActionResult Delete(int id)
        {
            var payment = _context.Payments
                .Include(p => p.Invoice).ThenInclude(i => i.Student)
                .FirstOrDefault(p => p.Id == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        // DELETE (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null) return NotFound();

            var invoiceId = payment.InvoiceId;
            var amount    = payment.Amount;

            using var tx = _context.Database.BeginTransaction();
            try
            {
                _context.Payments.Remove(payment);
                _audit.Log("Delete", "Payment", id,
                    $"Deleted payment of {amount} for Invoice ID: {invoiceId}");
                _context.SaveChanges();

                _invoiceCtrl.SyncInvoiceStatus(invoiceId);

                tx.Commit();
                TempData["Success"] = "Payment successfully deleted.";
            }
            catch (DbUpdateException)
            {
                tx.Rollback();
                TempData["Error"] = "A database error occurred while deleting this payment.";
            }

            return RedirectToAction(nameof(Index));
        }

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
    }
}
