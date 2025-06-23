using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using File = CMS.Models.File;

namespace CMS.Pages.Contract
{
    public class PendingContractModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 6;
        public string? ErrorMessage { get; set; }

        public PendingContractModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Models.Contract> Contracts { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public bool IsLoaded { get; set; }

        public async Task OnGetAsync(int pageNumber = 1)
        {
            CurrentPage = pageNumber < 1 ? 1 : pageNumber;
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(email))
                {
                    ErrorMessage = "Không xác định được email người dùng hiện tại. Vui lòng đăng nhập lại.";
                    return;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ErrorMessage = "Không tìm thấy thông tin người dùng trong hệ thống.";
                    return;
                }

                var query = _context.Contracts
                    .Where(c => !c.IsDeleted && c.StaffID == user.Id && c.ContractStatus == "Pending");

                int totalContracts = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(totalContracts / (double)PageSize);

                Contracts = await query
                    .OrderByDescending(c => c.CreateDate)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .Include(c => c.Branch)
                    .Include(c => c.Company)
                    .Include(c => c.Partner)
                    .ToListAsync();
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
                var contract = await _context.Contracts
                    .Include(c => c.Partner)
                    .Include(c => c.Department)
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

                // Log action for viewing contract details
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
                    PartnerID = contract.PartnerID, // Include PartnerID for edit modal
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
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết hợp đồng: {ex.Message}" });
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
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải danh sách đối tác: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostAddContractAsync([FromForm] ContractRequestModel model, List<IFormFile> files, List<IFormFile> invoiceFiles)
        {
            // Explicitly remove DueDate from ModelState to bypass validation
            ModelState.Remove("DueDate");

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

                var contract = new Models.Contract
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

                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync(); // Save contract to get AutoID

                // Log contract creation
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
                    action: "Create",
                    note: $"Thêm mới hợp đồng: {contract.ContractName} (Mã: {contract.ContractNumber})",
                    data: contractData
                );

                // Handle invoice if provided
                if (!string.IsNullOrEmpty(model.InvoiceName))
                {
                    var invoice = new Models.Invoice
                    {
                        ContractID = contract.AutoID,
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
                    await _context.SaveChangesAsync(); // Save invoice to get AutoID

                    // Log invoice creation
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
                        note: $"Thêm hóa đơn: {invoice.InvoicesName} cho hợp đồng {contract.ContractNumber}",
                        data: invoiceData
                    );

                    // Handle invoice file attachments
                    if (invoiceFiles != null && invoiceFiles.Any())
                    {
                        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                        }

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
                                    ContractID = contract.AutoID,
                                    InvoiceID = invoice.AutoID,
                                    StaffID = user.Id,
                                    CorporationID = user.Corporation?.AutoID,
                                    CompanyID = user.Company?.AutoID,
                                    BranchID = user.Branch?.AutoID,
                                    DepartmentID = user.Department?.AutoID,
                                    PositionID = user.PositionID
                                };

                                _context.Files.Add(fileEntity);
                                await _context.SaveChangesAsync(); // Save file to get AutoID

                                // Log file addition
                                var fileData = JsonSerializer.Serialize(new
                                {
                                    fileEntity.AutoID,
                                    fileEntity.FilePath,
                                    fileEntity.FileType,
                                    ContractID = contract.AutoID,
                                    InvoiceID = invoice.AutoID
                                });
                                await LogAuditAsync(
                                    userId: user.Id,
                                    table: "Files",
                                    action: "Create",
                                    note: $"Thêm tệp đính kèm: {originalFileName} cho hóa đơn {invoice.InvoicesName} (Hợp đồng: {contract.ContractNumber})",
                                    data: fileData
                                );
                            }
                        }
                    }

                    // Update InvoicesAutoID
                    contract.InvoicesAutoID = invoice.AutoID;
                    await _context.SaveChangesAsync();
                }

                // Handle contract file attachments
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
                            await _context.SaveChangesAsync(); // Save file to get AutoID

                            // Log file addition
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
                    await _context.SaveChangesAsync(); // Update contract with FilesAutoID
                }

                return new JsonResult(new { success = true, id = contract.AutoID, partnerCompanyName = partner.CompanyName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm hợp đồng: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostEditContractAsync([FromForm] ContractRequestModel model, List<IFormFile> files)
        {
            // Explicitly remove DueDate from ModelState to bypass validation
            ModelState.Remove("DueDate");

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

                var contract = await _context.Contracts
                    .Include(c => c.Files)
                    .FirstOrDefaultAsync(c => c.AutoID == model.AutoID && !c.IsDeleted);

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

                // Update contract details
                contract.ContractName = model.ContractName;
                contract.ContractType = model.ContractType;
                contract.ContractNumber = model.ContractNumber;
                contract.ContractYear = model.ContractYear;
                contract.ContractDate = model.ContractDate;
                contract.ContractTime = model.ContractTime;
                contract.StartDate = DateTime.Parse(model.StartDate);
                contract.EndDate = DateTime.Parse(model.EndDate);
                contract.ContractValue = model.ContractValue ?? 0;
                contract.PartnerID = model.PartnerID;
                contract.LastUpdateBy = user.Email;
                contract.LastUpdateDate = DateTime.Now;

                // Handle file uploads
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

                            // Log file addition
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

                // Log contract update
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

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, id = contract.AutoID, partnerCompanyName = partner.CompanyName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật hợp đồng: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostAddFileInvoiceAsync([FromForm] ContractRequestModel model, List<IFormFile> files, List<IFormFile> invoiceFiles)
        {
            // Explicitly remove DueDate from ModelState to bypass validation
            ModelState.Remove("DueDate");

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

                var contract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.AutoID == model.ContractId && !c.IsDeleted);

                if (contract == null)
                {
                    return BadRequest(new { success = false, message = "Hợp đồng không tồn tại hoặc đã bị xóa." });
                }

                // Handle contract file uploads
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

                            // Log file addition
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

                // Handle invoice creation
                if (!string.IsNullOrEmpty(model.InvoiceName))
                {
                    var invoice = new Models.Invoice
                    {
                        ContractID = contract.AutoID,
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

                    // Log invoice creation
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
                        note: $"Thêm hóa đơn: {invoice.InvoicesName} cho hợp đồng {contract.ContractNumber}",
                        data: invoiceData
                    );

                    // Handle invoice file uploads
                    if (invoiceFiles != null && invoiceFiles.Any())
                    {
                        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                        }

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
                                    ContractID = contract.AutoID,
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

                                // Log file addition
                                var fileData = JsonSerializer.Serialize(new
                                {
                                    fileEntity.AutoID,
                                    fileEntity.FilePath,
                                    fileEntity.FileType,
                                    ContractID = contract.AutoID,
                                    InvoiceID = invoice.AutoID
                                });
                                await LogAuditAsync(
                                    userId: user.Id,
                                    table: "Files",
                                    action: "Create",
                                    note: $"Thêm tệp đính kèm: {originalFileName} cho hóa đơn {invoice.InvoicesName} (Hợp đồng: {contract.ContractNumber})",
                                    data: fileData
                                );
                            }
                        }
                    }

                    // Update InvoicesAutoID
                    contract.InvoicesAutoID = invoice.AutoID;
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm tệp/hoá đơn: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostDeleteContractAsync([FromBody] DeleteContractModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var contract = await _context.Contracts.FindAsync(model.Id);
                if (contract == null)
                {
                    return NotFound();
                }

                contract.IsDeleted = true;
                contract.IsActive = false;

                // Log contract deletion
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

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa hợp đồng: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostRemoveFileAsync([FromBody] DeleteFileModel model)
        {
            try
            {
                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.AutoID == model.FileId && !f.IsDeleted);

                if (file == null)
                {
                    return BadRequest(new { success = false, message = "Tệp không tồn tại hoặc đã bị xóa." });
                }

                file.IsDeleted = true;
                file.IsActive = false;
                file.LastUpdateDate = DateTime.Now;

                var email = User.FindFirstValue(ClaimTypes.Email);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    file.LastUpdateBy = user.Email;

                    // Log file deletion
                    var fileData = JsonSerializer.Serialize(new
                    {
                        file.AutoID,
                        file.FilePath,
                        file.ContractID,
                        file.InvoiceID
                    });
                    await LogAuditAsync(
                        userId: user.Id,
                        table: "Files",
                        action: "Delete",
                        note: $"Xóa tệp: {Path.GetFileName(file.FilePath)} (ContractID: {file.ContractID}, InvoiceID: {file.InvoiceID})",
                        data: fileData
                    );
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa tệp: {ex.Message}" });
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

                // Log file download
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
            public int Id { get; set; }
        }

        public class DeleteFileModel
        {
            public int FileId { get; set; }
        }

        public class ContractRequestModel
        {
            public int? AutoID { get; set; } // For editing contract
            public int? ContractId { get; set; } // For adding files/invoices
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
            // Invoice fields
            public string? InvoiceName { get; set; }
            public string? IssueDate { get; set; }
            public string? DueDate { get; set; } // Explicitly nullable, no [Required]
            public double? InvoiceValue { get; set; }
            public string? InvoiceStatus { get; set; }
        }
    }
}