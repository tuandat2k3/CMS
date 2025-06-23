using CMS.Configuration;
using CMS.Data;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace CMS.Pages.Contract
{
    public class ExpiringContractModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;

        public ExpiringContractModel(ApplicationDbContext context, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
        }

        public List<Models.Contract> Contracts { get; set; } = new();
        private const int PageSize = 10;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        public async Task OnGetAsync(int pageNumber = 1)
        {
            CurrentPage = pageNumber < 1 ? 1 : pageNumber;
            var today = DateTime.Today;
            var startDate = today.AddDays(1);
            var endDate = today.AddDays(30);
            var query = _context.Contracts
                .Where(c => !c.IsDeleted && c.EndDate.HasValue
                            && c.EndDate.Value.Date >= startDate
                            && c.EndDate.Value.Date <= endDate)
                .OrderBy(c => c.EndDate);

            int totalContracts = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalContracts / (double)PageSize);

            Contracts = await query
                .OrderBy(c => EF.Functions.DateDiffDay(DateTime.Today, c.EndDate.Value))
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
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

        public async Task<IActionResult> OnPostSendExpiryNotificationAsync(int id)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.AutoID == id && !c.IsDeleted);

            if (contract == null || string.IsNullOrEmpty(contract.StaffID))
                return new JsonResult(new { success = false, message = "Không tìm thấy hợp đồng hoặc nhân viên." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == contract.StaffID);
            if (user == null || string.IsNullOrEmpty(user.Email))
                return new JsonResult(new { success = false, message = "Không tìm thấy email của nhân viên." });

            try
            {
                using var smtp = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                {
                    Credentials = new NetworkCredential(_emailSettings.FromEmail, _emailSettings.Password),
                    EnableSsl = true
                };

                var subject = $"[Cảnh báo] Hợp đồng '{contract.ContractName}' sắp hết hạn";
                var body = $@"
                    Xin chào {user.FullName},<br><br>
                    Hệ thống thông báo hợp đồng <strong>{contract.ContractName}</strong> 
                    (Số: <strong>{contract.ContractNumber}</strong>) do bạn phụ trách sắp hết hạn vào ngày <strong>{contract.EndDate:dd/MM/yyyy}</strong>.<br><br>
                    <u>Thông tin chi tiết:</u><br>
                    - Ngày ký: <strong>{contract.StartDate:dd/MM/yyyy}</strong><br>
                    - Ngày hết hạn: <strong>{contract.EndDate:dd/MM/yyyy}</strong><br>
                    - Loại: <strong>{contract.ContractType}</strong><br><br>
                    Vui lòng kiểm tra và xử lý hợp đồng trước khi hết hạn.<br><br>
                    Trân trọng,<br><strong>Hệ thống Quản lý Hợp đồng</strong>";

                var mail = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8
                };

                mail.To.Add(user.Email);
                await smtp.SendMailAsync(mail);

                return new JsonResult(new { success = true, message = "Email đã được gửi thành công." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi gửi email: {ex.Message}" });
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
    }
}