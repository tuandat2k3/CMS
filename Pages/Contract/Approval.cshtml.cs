using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CMS.Pages.Contract
{
    public class ApprovalModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        public ApprovalModel(ApplicationDbContext context, ILogger<ApprovalModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Models.Contract> Contracts { get; set; } = new List<Models.Contract>();
        public string? ErrorMessage { get; set; }
        public bool IsLoaded { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(email))
                {
                    ErrorMessage = "Cannot determine current user email. Please log in again.";
                    _logger.LogWarning("User email is null or empty.");
                    return;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ErrorMessage = "User information not found in the system.";
                    _logger.LogWarning("User not found for email: {Email}", email);
                    return;
                }

                Contracts = await _context.Contracts
                    .Where(c => !c.IsDeleted
                        && c.DepartmentID == user.DepartmentID
                        && c.ContractStatus == "Pending")
                    .Include(c => c.Branch)
                    .Include(c => c.Department)
                    .Include(c => c.Company)
                    .Include(c => c.Partner)
                    .OrderByDescending(c => c.CreateDate)
                    .ToListAsync();

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contracts for user email: {Email}", User.FindFirstValue(ClaimTypes.Email));
                ErrorMessage = $"Đã xảy ra lỗi khi tải dữ liệu: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetContractDetailAsync(int id)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Partner)
                    .Include(c => c.Branch)
                    .Include(c => c.Company)
                    .Include(c => c.Department)
                    .Include(c => c.Files)
                    .FirstOrDefaultAsync(c => c.AutoID == id && !c.IsDeleted);

                if (contract == null)
                {
                    _logger.LogWarning("Contract not found or deleted: {ContractId}", id);
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
                _logger.LogError(ex, "Error fetching contract detail: {ContractId}", id);
                return new JsonResult(new { success = false, message = $"Lỗi khi lấy chi tiết hợp đồng: {ex.Message}" });
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

        public async Task<IActionResult> OnPostApprovalContractAsync([FromBody] ApprovalRequest model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            var contract = await _context.Contracts.FindAsync(model.Id);
            if (contract == null)
            {
                _logger.LogWarning("Contract not found: {ContractId}", model.Id);
                return new JsonResult(new { success = false, message = "Hợp đồng không tồn tại" });
            }

            try
            {
                contract.IsDeleted = false;
                contract.IsActive = true;
                contract.LastUpdateBy = User.Identity?.Name ?? "System";
                contract.LastUpdateDate = DateTime.Now;
                contract.ContractStatus = "Effective";

                _context.Update(contract);
                await _context.SaveChangesAsync();

                // Ghi log cho hành động phê duyệt hợp đồng
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
                        action: "Approve",
                        note: $"Phê duyệt hợp đồng: {contract.ContractName} (Mã: {contract.ContractNumber})",
                        data: contractData
                    );
                }

                _logger.LogInformation("Contract approved successfully: {ContractId}", contract.AutoID);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving contract: {ContractId}", model.Id);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostRejectContractAsync([FromBody] ApprovalRequest model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            var contract = await _context.Contracts.FindAsync(model.Id);
            if (contract == null)
            {
                _logger.LogWarning("Contract not found: {ContractId}", model.Id);
                return new JsonResult(new { success = false, message = "Hợp đồng không tồn tại" });
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                _logger.LogWarning("Reject reason is empty for contract: {ContractId}", model.Id);
                return new JsonResult(new { success = false, message = "Vui lòng cung cấp lý do từ chối" });
            }

            try
            {
                contract.IsActive = false;
                contract.LastUpdateBy = User.Identity?.Name ?? "System";
                contract.LastUpdateDate = DateTime.Now;
                contract.ContractStatus = "Cancel";

                var rejection = new ContractRejection
                {
                    ContractId = model.Id,
                    Reason = model.Reason,
                    RejectedBy = User.Identity?.Name ?? "System",
                    RejectedDate = DateTime.Now
                };

                _context.ContractRejections.Add(rejection);
                _context.Update(contract);
                await _context.SaveChangesAsync();

                // Ghi log cho hành động từ chối hợp đồng
                var email = User.FindFirstValue(ClaimTypes.Email);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    var contractData = JsonSerializer.Serialize(new
                    {
                        contract.AutoID,
                        contract.ContractNumber,
                        contract.ContractName,
                        RejectionReason = model.Reason
                    });
                    await LogAuditAsync(
                        userId: user.Id,
                        table: "Contracts",
                        action: "Reject",
                        note: $"Từ chối hợp đồng: {contract.ContractName} (Mã: {contract.ContractNumber}). Lý do: {model.Reason}",
                        data: contractData
                    );
                }

                _logger.LogInformation("Contract rejected successfully: {ContractId} with reason: {Reason}", contract.AutoID, model.Reason);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting contract: {ContractId}", model.Id);
                return new JsonResult(new { success = false, message = ex.Message });
            }
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

        public class ApprovalRequest
        {
            public int Id { get; set; }
            public string? Reason { get; set; }
        }
    }
}