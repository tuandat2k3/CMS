using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Pages.Invoice
{
    public class CurrentInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public CurrentInvoiceModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public List<Models.Invoice> Invoices { get; set; } = new();
        public List<Models.Contract> Contracts { get; set; } = new();
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public string PriceRange { get; set; } = ""; 
        [BindProperty(SupportsGet = true)]
        public int? ContractID { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return Unauthorized();
            var userId = user.Id;
            var userContractIds = await _context.Contracts
                .Where(c => c.StaffID == userId && (c.IsDeleted == false || c.IsDeleted == null))
                .Select(c => c.AutoID)
                .ToListAsync();
            var query = _context.Invoices
                .Include(i => i.Contract)
                .Where(i =>
                    i.ContractID != null &&
                    userContractIds.Contains(i.ContractID.Value) &&
                    (i.IsDeleted == false || i.IsDeleted == null))
                .AsQueryable();
            if (StartDate.HasValue)
                query = query.Where(i => i.IssueDate >= StartDate.Value);
            if (EndDate.HasValue)
                query = query.Where(i => i.IssueDate <= EndDate.Value);
            // Lọc theo khoảng giá trị
            if (!string.IsNullOrEmpty(PriceRange))
            {
                query = query.Where(i => i.InvoiceValue.HasValue && (
                    (PriceRange == "Under1M" && (decimal)i.InvoiceValue.Value >= 0m && (decimal)i.InvoiceValue.Value < 1_000_000m) ||
                    (PriceRange == "1MTo5M" && (decimal)i.InvoiceValue.Value >= 1_000_000m && (decimal)i.InvoiceValue.Value <= 5_000_000m) ||
                    (PriceRange == "5MTo10M" && (decimal)i.InvoiceValue.Value > 5_000_000m && (decimal)i.InvoiceValue.Value <= 10_000_000m) ||
                    (PriceRange == "Above10M" && (decimal)i.InvoiceValue.Value > 10_000_000m)
                ));
            }
            if (ContractID.HasValue)
                query = query.Where(i => i.ContractID == ContractID);
            Invoices = await query.OrderByDescending(i => i.IssueDate).ToListAsync();
            // Lấy tất cả hợp đồng để hiển thị trong dropdown
            Contracts = await _context.Contracts
                .Where(c => userContractIds.Contains(c.AutoID))
                .ToListAsync();
            return Page();
        }
        public async Task<IActionResult> OnGetInvoiceDetailAsync(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Contract)
                    .Include(i => i.Files)
                    .FirstOrDefaultAsync(i => i.AutoID == id && !i.IsDeleted);
                if (invoice == null)
                {
                    return new JsonResult(new { success = false, message = "Hóa đơn không tồn tại hoặc đã bị xóa" });
                }
                var invoiceDetail = new
                {
                    invoice.AutoID,
                    invoice.InvoicesName,
                    contractName = invoice.Contract?.ContractName,
                    invoice.IssueDate,
                    invoice.DueDate,
                    invoice.InvoiceValue,
                    invoice.Status,
                    invoice.CreateBy,
                    invoice.CreateDate,
                    invoice.LastUpdateBy,
                    invoice.LastUpdateDate,
                    invoiceFiles = invoice.Files?.Where(f => !f.IsDeleted).Select(f => new
                    {
                        f.AutoID,
                        fileName = Path.GetFileName(f.FilePath),
                        f.FileType
                    }).ToList()
                };

                return new JsonResult(new { success = true, invoice = invoiceDetail });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết hóa đơn: {ex.Message}" });
            }
        }
        public async Task<IActionResult> OnGetDownloadFileAsync(int id)
        {
            try
            {
                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.AutoID == id && !f.IsDeleted);

                if (file == null)
                {
                    return NotFound();
                }
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileName = Path.GetFileName(file.FilePath);
                return File(fileBytes, GetContentType(file.FileType), fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải tệp: {ex.Message}" });
            }
        }
        private string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}