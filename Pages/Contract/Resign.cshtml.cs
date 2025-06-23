using System.Security.Claims;
using CMS.Configuration;
using CMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using File = CMS.Models.File;
using System.Text.Json;
using CMS.Models;

namespace CMS.Pages.Contract
{
    public class ResignModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResignModel(ApplicationDbContext context, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
        }

        public List<Models.Contract> Contracts { get; set; } = new();
        private const int PageSize = 10;
        public string? ErrorMessage { get; set; }
        public bool IsLoaded { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        public async Task OnGetAsync(DateTime? startDateFrom, DateTime? startDateTo, string status, string keyword, string type, double? minValue, double? maxValue, int pageNumber = 1)
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
                CurrentPage = pageNumber < 1 ? 1 : pageNumber;
                var today = DateTime.Today;
                var startDate = today.AddDays(1);
                var endDate = today.AddDays(30);
                var query = _context.Contracts
                    .Where(c => !c.IsDeleted && c.EndDate.HasValue
                                && c.StaffID == user.Id
                                && c.ContractStatus != "Pending"
                                && c.ContractStatus != "Cancel"
                                && c.ContractStatus != "Completed"
                                && c.EndDate.Value.Date >= startDate
                                && c.EndDate.Value.Date <= endDate)
                    .Include(c => c.Branch)
                    .Include(c => c.Company)
                    .Include(c => c.Partner)
                    .Include(c => c.Department)
                    .OrderBy(c => c.EndDate)
                    .AsQueryable();

                if (startDateFrom.HasValue)
                    query = query.Where(c => c.StartDate >= startDateFrom.Value);

                if (startDateTo.HasValue)
                    query = query.Where(c => c.StartDate <= startDateTo.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(c => c.ContractStatus == status);

                if (!string.IsNullOrEmpty(keyword))
                    query = query.Where(c => c.ContractName.Contains(keyword));

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(c => c.ContractType == type);

                if (minValue.HasValue)
                    query = query.Where(c => c.ContractValue >= minValue);

                if (maxValue.HasValue)
                    query = query.Where(c => c.ContractValue <= maxValue);

                int totalContracts = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(totalContracts / (double)PageSize);

                Contracts = await query
                    .OrderBy(c => EF.Functions.DateDiffDay(DateTime.Today, c.EndDate.Value))
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
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
                    .FirstOrDefaultAsync(c => c.AutoID == id && c.StaffID == user.Id);

                if (contract == null)
                {
                    return new JsonResult(new { success = false, message = "Hợp đồng không tồn tại hoặc đã bị xóa" });
                }

                // Calculate total invoice value for Paid invoices only
                var totalInvoiceValue = await _context.Invoices
                    .Where(i => i.ContractID == id && !i.IsDeleted && i.Status == "Paid")
                    .SumAsync(i => i.InvoiceValue ?? 0);

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
                        note: $"Xem chi tiết hợp đồng sắp hết hạn: {contract.ContractName} (Mã: {contract.ContractNumber})",
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
                    PartnerID = contract.Partner?.AutoID,
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
                return new JsonResult(new { success = false, message = $"Lỗi khi lấy chi tiết hợp đồng: {ex.Message}" });
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

        public async Task<IActionResult> OnPostReSignContractAsync([FromForm] ContractRequestModel model, List<IFormFile> files, List<IFormFile> invoiceFiles)
        {
            ModelState.Remove("DueDate");
            ModelState.Remove("AutoID");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { success = false, message = "Validation failed", errors });
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
                    .Include(u => u.Corporation)
                    .Include(u => u.Position)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin người dùng trong hệ thống." });
                }

                var originalContract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.AutoID == model.ContractId && !c.IsDeleted && c.StaffID == user.Id);

                if (originalContract == null)
                {
                    return BadRequest(new { success = false, message = "Hợp đồng không tồn tại hoặc bạn không có quyền truy cập." });
                }

                if (!model.PartnerID.HasValue)
                {
                    return BadRequest(new { success = false, message = "Đối tác không được để trống." });
                }

                var partner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.AutoID == model.PartnerID.Value && !p.IsDeleted);

                if (partner == null)
                {
                    return BadRequest(new { success = false, message = "Đối tác không tồn tại hoặc đã bị xóa." });
                }

                if (files == null || !files.Any())
                {
                    return BadRequest(new { success = false, message = "Vui lòng tải lên ít nhất một tệp đính kèm hợp đồng." });
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".png" };
                var maxFileSize = 10 * 1024 * 1024; // 10MB
                foreach (var file in files)
                {
                    var fileExtension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(new { success = false, message = $"Loại tệp {fileExtension} không được phép." });
                    }
                    if (file.Length > maxFileSize)
                    {
                        return BadRequest(new { success = false, message = "Kích thước tệp vượt quá giới hạn 10MB." });
                    }
                }

                if (invoiceFiles != null && invoiceFiles.Any())
                {
                    foreach (var file in invoiceFiles)
                    {
                        var fileExtension = Path.GetExtension(file.FileName).ToLower();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            return BadRequest(new { success = false, message = $"Loại tệp {fileExtension} không được phép cho hóa đơn." });
                        }
                        if (file.Length > maxFileSize)
                        {
                            return BadRequest(new { success = false, message = "Kích thước tệp hóa đơn vượt quá giới hạn 10MB." });
                        }
                    }
                }

                var newContract = new Models.Contract
                {
                    ContractName = model.ContractName,
                    ContractType = model.ContractType,
                    ContractNumber = model.ContractNumber,
                    ContractYear = model.ContractYear,
                    ContractDate = model.ContractDate,
                    ContractTime = model.ContractTime,
                    StartDate = DateTime.Parse(model.StartDate),
                    EndDate = DateTime.Parse(model.EndDate),
                    ContractValue = model.ContractValue ?? 0,
                    ContractStatus = "Pending",
                    IsActive = false,
                    IsDeleted = false,
                    CreateBy = user.FullName,
                    CreateDate = DateTime.Now,
                    StaffID = user.Id,
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
                    AssignmentsAutoID = null,
                    Files = new List<File>()
                };

                _context.Contracts.Add(newContract);
                await _context.SaveChangesAsync();

                var contractData = JsonSerializer.Serialize(new
                {
                    newContract.AutoID,
                    newContract.ContractNumber,
                    newContract.ContractName,
                    newContract.ContractType,
                    newContract.PartnerID
                });
                await LogAuditAsync(
                    userId: user.Id,
                    table: "Contracts",
                    action: "ReSign",
                    note: $"Tái ký hợp đồng: {newContract.ContractName} (Mã: {newContract.ContractNumber}) từ hợp đồng cũ (Mã: {originalContract.ContractNumber})",
                    data: contractData
                );

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
                        var fileName = $"{newContract.AutoID}_{originalFileName}";
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
                            ContractID = newContract.AutoID,
                            StaffID = user.Id,
                            CorporationID = user.Corporation?.AutoID,
                            CompanyID = user.Company?.AutoID,
                            BranchID = user.Branch?.AutoID,
                            DepartmentID = user.Department?.AutoID,
                            PositionID = user.PositionID
                        };

                        _context.Files.Add(fileEntity);
                        await _context.SaveChangesAsync();

                        var fileData = JsonSerializer.Serialize(new
                        {
                            fileEntity.AutoID,
                            fileEntity.FilePath,
                            fileEntity.FileType,
                            ContractID = newContract.AutoID
                        });
                        await LogAuditAsync(
                            userId: user.Id,
                            table: "Files",
                            action: "Create",
                            note: $"Thêm tệp đính kèm: {originalFileName} cho hợp đồng {newContract.ContractNumber}",
                            data: fileData
                        );

                        if (newContract.FilesAutoID == null)
                        {
                            newContract.FilesAutoID = fileEntity.AutoID;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(model.InvoiceName))
                {
                    var invoice = new Models.Invoice
                    {
                        ContractID = newContract.AutoID,
                        InvoicesName = model.InvoiceName,
                        IssueDate = string.IsNullOrEmpty(model.IssueDate) ? null : DateTime.Parse(model.IssueDate),
                        DueDate = string.IsNullOrEmpty(model.DueDate) ? null : DateTime.Parse(model.DueDate),
                        InvoiceValue = model.InvoiceValue,
                        Status = string.IsNullOrEmpty(model.InvoiceStatus) ? "Pending" : model.InvoiceStatus,
                        IsDeleted = false,
                        IsActive = true,
                        CreateBy = user.FullName,
                        CreateDate = DateTime.Now,
                        LastUpdateBy = user.Email,
                        LastUpdateDate = DateTime.Now,
                        Files = new List<File>()
                    };

                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    var invoiceData = JsonSerializer.Serialize(new
                    {
                        invoice.AutoID,
                        invoice.InvoicesName,
                        invoice.ContractID
                    });
                    await LogAuditAsync(
                        userId: user.Id,
                        table: "Invoices",
                        action: "Create",
                        note: $"Thêm hóa đơn: {invoice.InvoicesName} cho hợp đồng {newContract.ContractNumber}",
                        data: invoiceData
                    );

                    if (invoiceFiles != null && invoiceFiles.Any())
                    {
                        foreach (var file in invoiceFiles)
                        {
                            if (file.Length > 0)
                            {
                                var originalFileName = Path.GetFileName(file.FileName);
                                var fileName = $"invoice_{invoice.AutoID}_{originalFileName}";
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
                                    ContractID = newContract.AutoID,
                                    InvoiceID = invoice.AutoID,
                                    StaffID = user.Id,
                                    CorporationID = user.Corporation?.AutoID,
                                    CompanyID = user.Company?.AutoID,
                                    BranchID = user.Branch?.AutoID,
                                    DepartmentID = user.Department?.AutoID,
                                    PositionID = user.PositionID
                                };

                                _context.Files.Add(fileEntity);
                                await _context.SaveChangesAsync();

                                var fileData = JsonSerializer.Serialize(new
                                {
                                    fileEntity.AutoID,
                                    fileEntity.FilePath,
                                    fileEntity.FileType,
                                    ContractID = newContract.AutoID,
                                    InvoiceID = invoice.AutoID
                                });
                                await LogAuditAsync(
                                    userId: user.Id,
                                    table: "Files",
                                    action: "Create",
                                    note: $"Thêm tệp đính kèm: {originalFileName} cho hóa đơn {invoice.InvoicesName} (Hợp đồng: {newContract.ContractNumber})",
                                    data: fileData
                                );
                            }
                        }
                    }

                    newContract.InvoicesAutoID = invoice.AutoID;
                    await _context.SaveChangesAsync();
                }

                originalContract.ContractStatus = "Completed";
                originalContract.LastUpdateBy = user.Email;
                originalContract.LastUpdateDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, id = newContract.AutoID, partnerCompanyName = partner.CompanyName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi tái ký hợp đồng: {ex.Message}" });
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
            public int? ContractId { get; set; }
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
            public string? InvoiceName { get; set; }
            public string? IssueDate { get; set; }
            public string? DueDate { get; set; }
            public double? InvoiceValue { get; set; }
            public string? InvoiceStatus { get; set; }
        }
    }
}