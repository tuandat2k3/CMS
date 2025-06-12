using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Pages.Manager
{
    public class CompanyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyModel> _logger;
        public string? ErrorMessage { get; set; }

        public CompanyModel(ApplicationDbContext context, ILogger<CompanyModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Company> Companies { get; set; } = new();
        public bool IsLoaded { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                Companies = await _context.Companies
                    .Include(c => c.Corporation)
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();
                _logger.LogInformation("Đã tải {Count} công ty", Companies.Count);
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách công ty");
                ErrorMessage = $"Đã xảy ra lỗi khi tải dữ liệu: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.Corporation)
                    .FirstOrDefaultAsync(c => c.AutoID == id);

                if (company == null)
                {
                    _logger.LogWarning("Không tìm thấy công ty: Id={Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("Đã lấy chi tiết công ty: Id={Id}", id);
                return new JsonResult(new
                {
                    company.AutoID,
                    company.CompanyName,
                    company.CompanySymbol,
                    company.Address,
                    company.City,
                    company.Country,
                    company.Phone,
                    company.HomePhone,
                    company.Representative,
                    company.IsActive,
                    CorporationName = company.Corporation?.CorporationName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết công ty: Id={Id}", id);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải chi tiết: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostAddCompanyAsync([FromBody] AddCompanyModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Dữ liệu không hợp lệ khi thêm công ty: {Errors}", string.Join("; ", errors));
                return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
            }

            try
            {
                _logger.LogInformation("Thêm công ty mới: {CompanyName}", model.CompanyName);
                var company = new Company
                {
                    CompanyName = model.CompanyName,
                    CompanySymbol = model.CompanySymbol,
                    Address = model.Address,
                    City = model.City,
                    Country = model.Country,
                    Phone = model.Phone,
                    HomePhone = model.HomePhone,
                    Representative = model.Representative,
                    CorporationID = model.CorporationID,
                    IsActive = model.IsActive,
                    CreateBy = User.Identity?.Name ?? "System",
                    CreateDate = DateTime.Now,
                    LastModifiedBy = User.Identity?.Name ?? "System",
                    LastModifiedDate = DateTime.Now
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Thêm công ty thành công: Id={Id}", company.AutoID);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm công ty: {CompanyName}", model.CompanyName);
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm công ty: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostUpdateCompanyAsync([FromBody] UpdateCompanyModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Dữ liệu không hợp lệ khi cập nhật công ty: {Errors}", string.Join("; ", errors));
                return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
            }

            try
            {
                _logger.LogInformation("Cập nhật công ty: Id={Id}", model.AutoID);
                var company = await _context.Companies.FindAsync(model.AutoID);
                if (company == null)
                {
                    _logger.LogWarning("Không tìm thấy công ty: Id={Id}", model.AutoID);
                    return NotFound(new { success = false, message = "Công ty không tồn tại" });
                }

                company.CompanyName = model.CompanyName;
                company.CompanySymbol = model.CompanySymbol;
                company.Address = model.Address;
                company.City = model.City;
                company.Country = model.Country;
                company.Phone = model.Phone;
                company.HomePhone = model.HomePhone;
                company.Representative = model.Representative;
                company.CorporationID = model.CorporationID;
                company.IsActive = model.IsActive;
                company.LastModifiedBy = User.Identity?.Name ?? "System";
                company.LastModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cập nhật công ty thành công: Id={Id}", model.AutoID);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật công ty: Id={Id}", model.AutoID);
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật công ty: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostDeleteCompanyAsync([FromBody] DeleteCompanyModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Dữ liệu không hợp lệ khi xóa công ty: {Errors}", string.Join("; ", errors));
                return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
            }

            try
            {
                _logger.LogInformation("Xóa công ty: Id={Id}", model.AutoID);
                var company = await _context.Companies.FindAsync(model.AutoID);
                if (company == null)
                {
                    _logger.LogWarning("Không tìm thấy công ty: Id={Id}", model.AutoID);
                    return NotFound(new { success = false, message = "Công ty không tồn tại" });
                }

                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Xóa công ty thành công: Id={Id}", model.AutoID);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa công ty: Id={Id}", model.AutoID);
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa công ty: {ex.Message}" });
            }
        }

        public class AddCompanyModel
        {
            [Required(ErrorMessage = "Tên công ty là bắt buộc")]
            public string CompanyName { get; set; }
            public string? CompanySymbol { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
            public string? Phone { get; set; }
            public string? HomePhone { get; set; }
            public string? Representative { get; set; }
            public int? CorporationID { get; set; }
            [Required]
            public bool IsActive { get; set; }
        }

        public class UpdateCompanyModel
        {
            [Required]
            public int AutoID { get; set; }
            [Required(ErrorMessage = "Tên công ty là bắt buộc")]
            public string CompanyName { get; set; }
            public string? CompanySymbol { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
            public string? Phone { get; set; }
            public string? HomePhone { get; set; }
            public string? Representative { get; set; }
            public int? CorporationID { get; set; }
            [Required]
            public bool IsActive { get; set; }
        }

        public class DeleteCompanyModel
        {
            [Required]
            public int AutoID { get; set; }
        }
    }
}