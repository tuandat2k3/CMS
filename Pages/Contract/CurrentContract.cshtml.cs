using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using File = CMS.Models.File;

namespace CMS.Pages.Contract
{
    public class CurrentContractModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CurrentContractModel(ApplicationDbContext context)
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
                    ErrorMessage = "Không thể xác định email người dùng. Vui lòng đăng nhập lại.";
                    return;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ErrorMessage = "Không tìm thấy thông tin người dùng trong hệ thống.";
                    return;
                }

                Contracts = await _context.Contracts
                    .Where(c => !c.IsDeleted
                        && c.DepartmentID == user.DepartmentID
                        && c.ContractStatus != "Preparing"
                        && c.ContractStatus != "Assigned"
                        && c.ContractStatus != "Pending"
                        && c.ContractStatus != "Cancel")
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
                ErrorMessage = $"Đã xảy ra lỗi khi tải dữ liệu: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetContractDetailAsync(int id)
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                var contract = await _context.Contracts
                    .Include(c => c.Branch)
                    .Include(c => c.Company)
                    .Include(c => c.Partner)
                    .Include(c => c.Department)
                    .Include(c => c.Position)
                    .Include(c => c.Files)
                    .FirstOrDefaultAsync(c => c.AutoID == id && c.DepartmentID == user.DepartmentID);

                if (contract == null)
                {
                    return new JsonResult(new { success = false, message = "Hợp đồng không tồn tại hoặc đã bị xóa" });
                }

                // Calculate total invoice value for Paid invoices only
                var totalInvoiceValue = await _context.Invoices
                    .Where(i => i.ContractID == id && !i.IsDeleted && i.Status == "Paid")
                    .SumAsync(i => i.InvoiceValue ?? 0);

                // Ghi log cho hành động xem chi tiết hợp đồng
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
                return new JsonResult(new { success = false, message = $"Lỗi khi lấy chi tiết hợp đồng: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetPartnersAsync()
        {
            try
            {
                var partners = await _context.Partners
                    .Where(p => !p.IsDeleted)
                    .Select(p => new { id = p.AutoID, companyName = p.CompanyName })
                    .ToListAsync();
                return new JsonResult(partners);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi khi lấy danh sách đối tác: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetDepartmentStaffAsync()
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    return new JsonResult(new { success = false, message = "Không xác định được người dùng hiện tại." });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng trong hệ thống." });
                }

                var staff = await _context.Users
                    .Where(u => u.DepartmentID == user.DepartmentID)
                    .Select(u => new { staffID = u.Id, fullName = u.FullName })
                    .ToListAsync();

                return new JsonResult(staff);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi khi tải danh sách nhân viên: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostAssignedContractToUserAsync([FromBody] ContractRequestModel model)
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
                    return new JsonResult(new { success = false, message = "Không xác định được người dùng hiện tại." });
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
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng trong hệ thống." });
                }

                var staff = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.StaffID);
                if (staff == null)
                {
                    return new JsonResult(new { success = false, message = "Nhân viên phụ trách không tồn tại." });
                }

                string? partnerCompanyName = null;
                if (!model.PartnerID.HasValue)
                {
                    return new JsonResult(new { success = false, message = "Vui lòng chọn đối tác." });
                }

                var partner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.AutoID == model.PartnerID.Value && !p.IsDeleted);

                if (partner == null)
                {
                    return new JsonResult(new { success = false, message = "Đối tác không tồn tại hoặc đã bị xóa." });
                }
                partnerCompanyName = partner.CompanyName;

                var contract = new Models.Contract
                {
                    ContractName = model.ContractName,
                    ContractType = model.ContractType,
                    ContractNumber = model.ContractNumber,
                    ContractYear = model.ContractYear,
                    ContractDate = model.ContractDate,
                    ContractStatus = "Assigned",
                    IsActive = false,
                    IsDeleted = false,
                    CreateBy = user.FullName,
                    CreateDate = DateTime.Now,
                    StaffID = model.StaffID,
                    DepartmentID = user.Department?.AutoID,
                    BranchID = user.Branch?.AutoID,
                    CompanyID = user.Company?.AutoID,
                    CorporationID = user.Corporation?.AutoID,
                    PartnerID = model.PartnerID,
                    PositionID = user.PositionID,
                    LastUpdateBy = user.Email,
                    LastUpdateDate = DateTime.Now,
                    FilesAutoID = null,
                    InvoicesAutoID = null,
                    AssignmentsAutoID = null
                };

                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();

                var assignment = new Models.Assignment
                {
                    ContractID = contract.AutoID,
                    UserID = model.StaffID,
                    AssignTime = DateTime.Now,
                    IsActive = true,
                    IsDeleted = false,
                    CreateBy = user.FullName,
                    CreateDate = DateTime.Now,
                    LastUpdateBy = user.Email,
                    LastUpdateDate = DateTime.Now
                };

                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();

                contract.AssignmentsAutoID = assignment.AutoID;
                await _context.SaveChangesAsync();

                // Ghi log cho hành động gán hợp đồng
                var contractData = JsonSerializer.Serialize(new
                {
                    contract.AutoID,
                    contract.ContractNumber,
                    contract.ContractName,
                    AssignedToUserId = model.StaffID
                });
                await LogAuditAsync(
                    userId: user.Id,
                    table: "Contracts",
                    action: "Assign",
                    note: $"Gán hợp đồng: {contract.ContractName} (Mã: {contract.ContractNumber}) cho nhân viên ID: {model.StaffID}",
                    data: contractData
                );

                return new JsonResult(new { success = true, id = contract.AutoID, partnerCompanyName });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi khi thêm hợp đồng: {ex.Message}" });
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
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi khi tải tệp: {ex.Message}" });
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

        public class ContractRequestModel
        {
            public string StaffID { get; set; }
            public string ContractName { get; set; }
            public string ContractType { get; set; }
            public string ContractNumber { get; set; }
            public string ContractYear { get; set; }
            public string ContractDate { get; set; }
            public int? PartnerID { get; set; }
        }
    }
}