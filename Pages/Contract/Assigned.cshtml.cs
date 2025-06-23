using CMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using File = CMS.Models.File;
using System.Text.Json;
using CMS.Models;

namespace CMS.Pages.Contract
{
    public class AssignedModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AssignedModel> _logger;

        public AssignedModel(ApplicationDbContext context, ILogger<AssignedModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Models.Contract> Contracts { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool IsLoaded { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(email))
                {
                    ErrorMessage = "Không thể xác định email người dùng. Vui lòng đăng nhập lại.";
                    _logger.LogWarning("No email found in user claims.");
                    return;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ErrorMessage = "Không tìm thấy thông tin người dùng trong hệ thống.";
                    _logger.LogWarning("User not found for email: {Email}", email);
                    return;
                }

                Contracts = await _context.Contracts
                    .Where(c => !c.IsDeleted
                        && c.StaffID == user.Id
                        && c.ContractStatus == "Assigned")
                    .Include(c => c.Branch)
                    .Include(c => c.Company)
                    .Include(c => c.Partner)
                    .OrderByDescending(c => c.LastUpdateDate)
                    .ToListAsync();

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Đã xảy ra lỗi khi tải dữ liệu: {ex.Message}";
                _logger.LogError(ex, "Error loading contracts");
            }
        }

        public async Task<IActionResult> OnGetContractDetailAsync(int id)
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                var contract = await _context.Contracts
                    .Include(c => c.Branch)
                    .Include(c => c.Company)
                    .Include(c => c.Partner)
                    .Include(c => c.Department)
                    .Include(c => c.Position)
                    .Include(c => c.Files)
                    .FirstOrDefaultAsync(c => c.AutoID == id && c.StaffID == user.Id);

                if (contract == null)
                {
                    _logger.LogWarning("Contract not found or deleted for ID: {Id}, User: {Email}", id, email);
                    return new JsonResult(new { success = false, message = "Hợp đồng không tồn tại hoặc đã bị xóa" });
                }

                // Ghi log cho hành động xem chi tiết hợp đồng
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
                    Files = contract.Files?.Select(f => new
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
                _logger.LogError(ex, "Error fetching contract details for ID: {Id}", id);
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

        public async Task<IActionResult> OnPostUpdateContractAsync([FromForm] ContractUpdateModel model, List<IFormFile> files)
        {
            _logger.LogInformation("Received contract update request: {@Model}", model);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("Model validation failed: {Errors}", string.Join("; ", errors));
                return new JsonResult(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join("; ", errors) });
            }

            if (model == null)
            {
                _logger.LogWarning("Received null model in UpdateContract");
                return new JsonResult(new { success = false, message = "Dữ liệu gửi lên không hợp lệ." });
            }

            if (model.StartDate == null)
            {
                _logger.LogWarning("StartDate is null");
                return new JsonResult(new { success = false, message = "Ngày bắt đầu không được để trống." });
            }
            if (model.EndDate == null)
            {
                _logger.LogWarning("EndDate is null");
                return new JsonResult(new { success = false, message = "Ngày kết thúc không được để trống." });
            }
            if (model.ContractValue <= 0)
            {
                _logger.LogWarning("ContractValue is invalid: {Value}", model.ContractValue);
                return new JsonResult(new { success = false, message = "Giá trị hợp đồng phải lớn hơn 0." });
            }
            if (string.IsNullOrEmpty(model.ContractTime))
            {
                _logger.LogWarning("ContractTime is empty");
                return new JsonResult(new { success = false, message = "Thời gian ký không được để trống." });
            }

            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("No email found in user claims");
                    return new JsonResult(new { success = false, message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
                }

                var user = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.Branch)
                    .Include(u => u.Company)
                    .Include(u => u.Corporation)
                    .Include(u => u.Position)
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                var contract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.AutoID == model.ContractId && c.StaffID == user.Id);
                if (contract == null)
                {
                    _logger.LogWarning("Contract not found for ID: {Id}, User: {Email}", model.ContractId, email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy hợp đồng hoặc bạn không có quyền cập nhật." });
                }

                // Update fields
                contract.StartDate = model.StartDate;
                contract.EndDate = model.EndDate;
                contract.ContractValue = model.ContractValue;
                contract.ContractTime = model.ContractTime;
                contract.ContractStatus = "Pending";
                contract.LastUpdateBy = user.Email;
                contract.LastUpdateDate = DateTime.Now;

                // Xử lý file nếu có
                if (files != null && files.Any())
                {
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var originalFileName = Path.GetFileName(file.FileName);
                            var fileName = $"{contract.AutoID}_{originalFileName}";
                            var filePath = Path.Combine(uploadsDir, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var fileEntity = new File
                            {
                                FilePath = $"/uploads/{fileName}",
                                FileType = Path.GetExtension(file.FileName).ToLower(),
                                IsDeleted = false,
                                IsActive = true,
                                CreateBy = user.FullName,
                                CreateDate = DateTime.Now,
                                LastUpdateBy = user.Email,
                                LastUpdateDate = DateTime.Now,
                                ContractID = contract.AutoID,
                                StaffID = user.Id,
                                CorporationID = user.Corporation?.AutoID,
                                CompanyID = user.Company?.AutoID,
                                BranchID = user.Branch?.AutoID,
                                DepartmentID = user.Department?.AutoID,
                                PositionID = user.PositionID
                            };

                            _context.Files.Add(fileEntity);
                            await _context.SaveChangesAsync();

                            // Ghi log cho hành động thêm tệp
                            var fileData = JsonSerializer.Serialize(new
                            {
                                fileEntity.AutoID,
                                fileEntity.FilePath,
                                fileEntity.FileType,
                                ContractID = contract.AutoID
                            });
                            await LogAuditAsync(
                                userId: user.Id,
                                table: "Files",
                                action: "Create",
                                note: $"Thêm tệp đính kèm: {originalFileName} cho hợp đồng {contract.ContractNumber}",
                                data: fileData
                            );

                            if (contract.FilesAutoID == null)
                            {
                                contract.FilesAutoID = fileEntity.AutoID;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Ghi log cho hành động cập nhật hợp đồng
                var contractData = JsonSerializer.Serialize(new
                {
                    contract.AutoID,
                    contract.ContractNumber,
                    contract.ContractName
                });
                await LogAuditAsync(
                    userId: user.Id,
                    table: "Contracts",
                    action: "Update",
                    note: $"Cập nhật hợp đồng: {contract.ContractName} (Mã: {contract.ContractNumber})",
                    data: contractData
                );

                _logger.LogInformation("Contract updated successfully: ID {Id}", model.ContractId);

                return new JsonResult(new { success = true, message = "Hợp đồng đã được cập nhật và gửi lên phê duyệt." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract ID: {Id}", model.ContractId);
                return new JsonResult(new { success = false, message = $"Lỗi khi cập nhật hợp đồng: {ex.Message}" });
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

        public class ContractUpdateModel
        {
            public int ContractId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public double ContractValue { get; set; }
            public string ContractTime { get; set; }
        }
    }
}