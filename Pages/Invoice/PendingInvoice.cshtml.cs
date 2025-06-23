using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CMS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.Data;

namespace CMS.Pages.Invoice
{
    public class PendingInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public PendingInvoiceModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Models.Invoice> PendingInvoices { get; set; }

        [BindProperty]
        public Models.Invoice Invoice { get; set; }

        public async Task OnGetAsync()
        {
            PendingInvoices = await _context.Invoices
                .Include(i => i.Contract)
                .Where(i => i.Status == "Pending" && i.IsActive && !i.IsDeleted)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var invoiceToUpdate = await _context.Invoices.FindAsync(Invoice.AutoID);
            if (invoiceToUpdate == null)
            {
                return NotFound();
            }

            invoiceToUpdate.InvoicesName = Invoice.InvoicesName;
            invoiceToUpdate.IssueDate = Invoice.IssueDate;
            invoiceToUpdate.DueDate = Invoice.DueDate;
            invoiceToUpdate.InvoiceValue = Invoice.InvoiceValue;
            invoiceToUpdate.LastUpdateBy = User.Identity.Name;
            invoiceToUpdate.LastUpdateDate = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Invoices.Any(e => e.AutoID == Invoice.AutoID))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkAsPaidAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            invoice.Status = "Paid";
            invoice.LastUpdateBy = User.Identity.Name;
            invoice.LastUpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}