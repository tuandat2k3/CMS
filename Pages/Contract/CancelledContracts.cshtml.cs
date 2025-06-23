using CMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
    public class CancelledContractsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CancelledContractsModel(ApplicationDbContext context)
        {
            _context = context;
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
                    ErrorMessage = "Cannot determine current user email. Please log in again.";
                    return;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ErrorMessage = "User information not found in the system.";
                    return;
                }

                Contracts = await _context.Contracts
                    .Where(c => !c.IsDeleted
                    && c.StaffID == user.Id
                    && c.ContractStatus == "Cancel")
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
            }
        }

        public async Task<IActionResult> OnGetContractDetailAsync(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            var contract = await _context.Contracts
                .Include(c => c.Branch)
                .Include(c => c.Company)
                .Include(c => c.Partner)
                .Include(c => c.Department)
                .Include(c => c.Position)
                .Include(c => c.ContractRejections)
                .Include(c => c.Files)
                .FirstOrDefaultAsync(c => c.AutoID == id && c.StaffID == user.Id);

            if (contract == null)
            {
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

            var latestRejection = contract.ContractRejections
                ?.OrderByDescending(r => r.RejectedDate)
                .FirstOrDefault();

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
                PartnerID = contract.Partner?.AutoID,
                PartnerName = contract.Partner?.CompanyName,
                CompanyName = contract.Company?.CompanyName,
                BranchName = contract.Branch?.BranchName,
                DepartmentName = contract.Department?.DepartmentName,
                contract.CreateBy,
                contract.CreateDate,
                contract.LastUpdateBy,
                contract.LastUpdateDate,
                contract.IsActive,
                RejectReason = latestRejection?.Reason,
                RejectedBy = latestRejection?.RejectedBy,
                RejectedDate = latestRejection?.RejectedDate,
                Files = contract.Files?.Select(f => new
                {
                    f.AutoID,
                    FileName = Path.GetFileName(f.FilePath),
                    f.FileType
                }).ToList()
            };

            return new JsonResult(new { success = true, contract = contractDetail });
        }

        public async Task<IActionResult> OnGetPartnersAsync()
        {
            var partners = await _context.Partners
                .Where(p => !p.IsDeleted)
                .Select(p => new { id = p.AutoID, companyName = p.CompanyName })
                .ToListAsync();
            return new JsonResult(partners);
        }

        public async Task<IActionResult> OnPostUpdateContractAsync([FromForm] ContractUpdateModel model, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { success = false, message = "Không xác định được người dùng hiện tại." });
                }

                var user = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.Branch)
                    .Include(u => u.Company)
                    .Include(u => u.Position)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin người dùng trong hệ thống." });
                }

                var contract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.AutoID == model.ContractId && c.StaffID == user.Id && !c.IsDeleted);

                if (contract == null)
                {
                    return BadRequest(new { success = false, message = "Hợp đồng không tồn tại hoặc đã bị xóa." });
                }

                if (!model.PartnerID.HasValue)
                {
                    return BadRequest(new { success = false, message = "Vui lòng chọn đối tác." });
                }

                var partner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.AutoID == model.PartnerID.Value && !p.IsDeleted);

                if (partner == null)
                {
                    return BadRequest(new { success = false, message = "Đối tác không tồn tại hoặc đã bị xóa." });
                }

                // Update contract fields
                contract.ContractName = model.ContractName;
                contract.ContractType = model.ContractType;
                contract.ContractNumber = model.ContractNumber;
                contract.ContractYear = model.ContractYear;
                contract.ContractDate = model.ContractDate;
                contract.ContractTime = model.ContractTime;
                contract.StartDate = DateTime.Parse(model.StartDate);
                contract.EndDate = DateTime.Parse(model.EndDate);
                contract.ContractValue = model.ContractValue ?? 0;
                contract.ContractStatus = "Pending";
                contract.PartnerID = model.PartnerID;
                contract.LastUpdateBy = user.Email;
                contract.LastUpdateDate = DateTime.UtcNow;

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
                    contract.ContractName,
                    contract.ContractType,
                    contract.PartnerID
                });
                await LogAuditAsync(
                    userId: user.Id,
                    table: "Contracts",
                    action: "Update",
                    note: $"Cập nhật hợp đồng: {contract.ContractName} (Mã: {contract.ContractNumber})",
                    data: contractData
                );

                return new JsonResult(new { success = true, message = "Cập nhật hợp đồng thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật hợp đồng: {ex.Message}" });
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
            public string ContractName { get; set; }
            public string ContractType { get; set; }
            public string ContractNumber { get; set; }
            public string ContractYear { get; set; }
            public string ContractDate { get; set; }
            public string ContractTime { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public double? ContractValue { get; set; }
            public int? PartnerID { get; set; }
        }
    }
}