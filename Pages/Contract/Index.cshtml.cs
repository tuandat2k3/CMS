using CMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using File = CMS.Models.File;
using System.Text.Json;
using System.Security.Claims;
using CMS.Models;

namespace CMS.Pages.Contract
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10;

        public IndexModel(ApplicationDbContext context) => _context = context;

        public List<Models.Contract> Contracts { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        public async Task OnGetAsync(DateTime? startDateFrom, DateTime? startDateTo, string? status, string? keyword, string? type, double? minValue, double? maxValue, int pageNumber = 1)
        {
            var query = _context.Contracts
                .Where(c => !c.IsDeleted
                    && c.EndDate > DateTime.Now
                    && c.ContractStatus != "Pending"
                    && c.ContractStatus != "Cancel"
                    && c.ContractStatus != "Completed"
                    && c.ContractStatus != "Preparing");

            if (startDateFrom.HasValue) query = query.Where(c => c.StartDate >= startDateFrom.Value);
            if (startDateTo.HasValue) query = query.Where(c => c.StartDate <= startDateTo.Value);
            if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.ContractStatus == status);
            if (!string.IsNullOrEmpty(keyword)) query = query.Where(c => c.ContractName.Contains(keyword));
            if (!string.IsNullOrEmpty(type)) query = query.Where(c => c.ContractType == type);
            if (minValue.HasValue) query = query.Where(c => c.ContractValue >= minValue);
            if (maxValue.HasValue) query = query.Where(c => c.ContractValue <= maxValue);

            var totalContracts = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalContracts / (double)PageSize);
            CurrentPage = Math.Clamp(pageNumber, 1, TotalPages);

            Contracts = await query
                .OrderByDescending(c => c.CreateDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetContractDetailAsync(int id)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Branch)
                    .Include(c => c.Company)
                    .Include(c => c.Partner)
                    .Include(c => c.Department)
                    .Include(c => c.Position)
                    .Include(c => c.Files)
                    .FirstOrDefaultAsync(c => c.AutoID == id && !c.IsDeleted);

                if (contract == null)
                {
                    return new JsonResult(new { success = false, message = "Hợp đồng không tồn tại hoặc đã bị xóa" });
                }

                // Calculate total invoice value for Paid invoices only
                var totalInvoiceValue = await _context.Invoices
                    .Where(i => i.ContractID == id && !i.IsDeleted && i.Status == "Paid")
                    .SumAsync(i => i.InvoiceValue ?? 0);

                // Ghi log cho hành động xem chi tiết hợp đồng
                var email = User.FindFirstValue(ClaimTypes.Email);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    var contractData = JsonSerializer.Serialize(new
                    {
                        contract.AutoID,
                        contract.ContractNumber,
                        contract.ContractName
                    });
                    await LogAuditAsync(
                        userId: user.Id,
                        table: "Contracts",
                        action: "View",
                        note: $"Xem chi tiết hợp đồng: {contract.ContractName} (Mã: {contract.ContractNumber})",
                        data: contractData
                    );
                }

                var contractDetail = new
                {
                    contract.AutoID,
                    contract.ContractNumber,
                    contract.ContractName,
                    contract.ContractType,
                    contract.ContractStatus,
                    contract.StartDate,
                    contract.EndDate,
                    contract.ContractValue,
                    contract.ContractYear,
                    contract.ContractDate,
                    contract.ContractTime,
                    PartnerName = contract.Partner?.CompanyName,
                    CompanyName = contract.Company?.CompanyName,
                    BranchName = contract.Branch?.BranchName,
                    DepartmentName = contract.Department?.DepartmentName,
                    contract.CreateBy,
                    contract.CreateDate,
                    contract.LastUpdateBy,
                    contract.LastUpdateDate,
                    contract.IsActive,
                    totalInvoiceValue,
                    ContractFiles = contract.Files?.Where(f => f.InvoiceID == null).Select(f => new
                    {
                        f.AutoID,
                        FileName = Path.GetFileName(f.FilePath),
                        f.FileType
                    }).ToList(),
                    InvoiceFiles = contract.Files?.Where(f => f.InvoiceID != null).Select(f => new
                    {
                        f.AutoID,
                        FileName = Path.GetFileName(f.FilePath),
                        f.FileType
                    }).ToList()
                };

                return new JsonResult(new { success = true, contract = contractDetail });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết hợp đồng: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetDownloadFileAsync(int id)
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

            // Ghi log cho hành động tải tệp
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                var fileData = JsonSerializer.Serialize(new
                {
                    file.AutoID,
                    file.FilePath,
                    file.ContractID
                });
                await LogAuditAsync(
                    userId: user.Id,
                    table: "Files",
                    action: "Download",
                    note: $"Tải tệp: {Path.GetFileName(file.FilePath)} (ContractID: {file.ContractID})",
                    data: fileData
                );
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(file.FilePath);

            return File(fileBytes, GetContentType(file.FileType), fileName);
        }

        public async Task<IActionResult> OnPostDeleteContractAsync([FromBody] DeleteContractModel model)
        {
            if (!TryValidateModel(model)) return BadRequest(ModelState);

            var contract = await _context.Contracts.FindAsync(model.Id);
            if (contract == null) return NotFound();

            contract.IsDeleted = true;
            contract.ContractStatus = "Decline";
            contract.LastUpdateBy = User.Identity?.Name ?? "System";
            contract.LastUpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // Ghi log cho hành động xóa hợp đồng
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                var contractData = JsonSerializer.Serialize(new
                {
                    contract.AutoID,
                    contract.ContractNumber,
                    contract.ContractName
                });
                await LogAuditAsync(
                    userId: user.Id,
                    table: "Contracts",
                    action: "Delete",
                    note: $"Xóa hợp đồng: {contract.ContractName} (Mã: {contract.ContractNumber})",
                    data: contractData
                );
            }

            return new JsonResult(new { success = true });
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

        private async Task LogAuditAsync(string userId, string table, string action, string note, string data)
        {
            var auditLog = new AuditLog
            {
                UserID = userId,
                Tables = table,
                Action = action,
                Note = note,
                Data = data,
                CreateDate = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public class DeleteContractModel
        {
            [Required] public int Id { get; set; }
        }

        public class ContractRequestModel
        {
            [Required] public string ContractName { get; set; } = "";
            [Required] public string ContractType { get; set; } = "";
            [Required] public string ContractNumber { get; set; } = "";
            public string? ContractYear { get; set; }
            public string? ContractDate { get; set; }
            public string? ContractTime { get; set; }
            [Required] public string StartDate { get; set; } = "";
            [Required] public string EndDate { get; set; } = "";
            [Range(0, double.MaxValue)] public double? ContractValue { get; set; }
            [Required] public string ContractStatus { get; set; } = "";
            public bool IsActive { get; set; }
        }
    }
}