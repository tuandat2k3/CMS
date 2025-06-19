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
using System.Security.Claims;
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
<<<<<<< HEAD
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Không tìm thấy email người dùng đăng nhập");
                    ErrorMessage = "Không thể xác định thông tin người dùng hiện tại.";
                    return;
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser == null || currentUser.CorporationID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CorporationID: Email={Email}", email);
                    ErrorMessage = "Không tìm thấy thông tin người dùng hoặc CorporationID.";
                    return;
                }

=======
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
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
<<<<<<< HEAD
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Không tìm thấy email người dùng đăng nhập");
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser == null || currentUser.CorporationID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CorporationID: Email={Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng hoặc CorporationID" });
                }

                var company = await _context.Companies
                    .Include(c => c.Corporation)
                    .FirstOrDefaultAsync(c => c.AutoID == id && c.CorporationID == currentUser.CorporationID);

                if (company == null)
                {
                    _logger.LogWarning("Không tìm thấy công ty hoặc không có quyền truy cập: Id={Id}", id);
                    return new JsonResult(new { success = false, message = "Không tìm thấy công ty hoặc không có quyền truy cập" });
=======
                var company = await _context.Companies
                    .Include(c => c.Corporation)
                    .FirstOrDefaultAsync(c => c.AutoID == id);

                if (company == null)
                {
                    _logger.LogWarning("Không tìm thấy công ty: Id={Id}", id);
                    return NotFound();
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
                }

                _logger.LogInformation("Đã lấy chi tiết công ty: Id={Id}", id);
                return new JsonResult(new
                {
<<<<<<< HEAD
                    success = true,
                    company = new
                    {
                        autoID = company.AutoID,
                        companyName = company.CompanyName,
                        companySymbol = company.CompanySymbol,
                        address = company.Address,
                        city = company.City,
                        country = company.Country,
                        phone = company.Phone,
                        homePhone = company.HomePhone,
                        representative = company.Representative,
                        isActive = company.IsActive ?? false,
                        corporationName = company.Corporation?.CorporationName
                    }
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
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi thêm công ty: {Errors}", string.Join("; ", errors));
                    return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
                }

                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Không tìm thấy email người dùng đăng nhập");
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser == null || currentUser.CorporationID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CorporationID: Email={Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng hoặc CorporationID" });
                }

                // Validate CompanySymbol uniqueness within the corporation
                if (!string.IsNullOrEmpty(model.CompanySymbol) && await _context.Companies
                    .AnyAsync(c => c.CompanySymbol == model.CompanySymbol && c.CorporationID == currentUser.CorporationID))
                {
                    _logger.LogWarning("Mã công ty đã tồn tại: {CompanySymbol}", model.CompanySymbol);
                    return new JsonResult(new { success = false, message = "Mã công ty đã tồn tại trong tập đoàn" });
                }

                _logger.LogInformation("Thêm công ty mới: {CompanyName}", model.CompanyName);
                var company = new Company
                {
                    CorporationID = currentUser.CorporationID,
                    CompanyName = model.CompanyName?.Trim(),
                    CompanySymbol = model.CompanySymbol?.Trim(),
                    Address = model.Address?.Trim(),
                    City = model.City?.Trim(),
                    Country = model.Country?.Trim(),
                    Phone = model.Phone?.Trim(),
                    HomePhone = model.HomePhone?.Trim(),
                    Representative = model.Representative?.Trim(),
                    IsActive = model.IsActive,
                    CreateBy = currentUser.Id,
                    CreateDate = DateTime.Now,
                    LastModifiedBy = currentUser.Id,
=======
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
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
                    LastModifiedDate = DateTime.Now
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Thêm công ty thành công: Id={Id}", company.AutoID);
<<<<<<< HEAD
                return new JsonResult(new { success = true, message = "Thêm công ty thành công" });
=======
                return new JsonResult(new { success = true });
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm công ty: {CompanyName}", model.CompanyName);
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm công ty: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostUpdateCompanyAsync([FromBody] UpdateCompanyModel model)
        {
<<<<<<< HEAD
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi cập nhật công ty: {Errors}", string.Join("; ", errors));
                    return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
                }

                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Không tìm thấy email người dùng đăng nhập");
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser == null || currentUser.CorporationID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CorporationID: Email={Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng hoặc CorporationID" });
                }

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.AutoID == model.AutoID && c.CorporationID == currentUser.CorporationID);
                if (company == null)
                {
                    _logger.LogWarning("Không tìm thấy công ty hoặc không có quyền cập nhật: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không tìm thấy công ty hoặc không có quyền cập nhật" });
                }

                // Validate CompanySymbol uniqueness within the corporation
                if (!string.IsNullOrEmpty(model.CompanySymbol) && await _context.Companies
                    .AnyAsync(c => c.CompanySymbol == model.CompanySymbol && c.CorporationID == currentUser.CorporationID && c.AutoID != model.AutoID))
                {
                    _logger.LogWarning("Mã công ty đã tồn tại: {CompanySymbol}", model.CompanySymbol);
                    return new JsonResult(new { success = false, message = "Mã công ty đã tồn tại trong tập đoàn" });
                }

                // Update fields, preserving existing CorporationID
                company.CompanyName = model.CompanyName?.Trim();
                company.CompanySymbol = model.CompanySymbol?.Trim();
                company.Address = model.Address?.Trim();
                company.City = model.City?.Trim();
                company.Country = model.Country?.Trim();
                company.Phone = model.Phone?.Trim();
                company.HomePhone = model.HomePhone?.Trim();
                company.Representative = model.Representative?.Trim();
                company.IsActive = model.IsActive;
                company.LastModifiedBy = currentUser.Id;
=======
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
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
                company.LastModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cập nhật công ty thành công: Id={Id}", model.AutoID);
<<<<<<< HEAD
                return new JsonResult(new { success = true, message = "Cập nhật công ty thành công" });
=======
                return new JsonResult(new { success = true });
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật công ty: Id={Id}", model.AutoID);
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật công ty: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostDeleteCompanyAsync([FromBody] DeleteCompanyModel model)
        {
<<<<<<< HEAD
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi xóa công ty: {Errors}", string.Join("; ", errors));
                    return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
                }

                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Không tìm thấy email người dùng đăng nhập");
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser == null || currentUser.CorporationID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CorporationID: Email={Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng hoặc CorporationID" });
                }

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.AutoID == model.AutoID && c.CorporationID == currentUser.CorporationID);
                if (company == null)
                {
                    _logger.LogWarning("Không tìm thấy công ty hoặc không có quyền xóa: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không tìm thấy công ty hoặc không có quyền xóa" });
                }

                // Check for dependencies
                if (await _context.Branches.AnyAsync(b => b.CompanyID == model.AutoID) ||
                    await _context.Users.AnyAsync(u => u.CompanyID == model.AutoID))
                {
                    _logger.LogWarning("Không thể xóa công ty vì đã được liên kết: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không thể xóa công ty vì đã được liên kết với chi nhánh hoặc người dùng" });
=======
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
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
                }

                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Xóa công ty thành công: Id={Id}", model.AutoID);
<<<<<<< HEAD
                return new JsonResult(new { success = true, message = "Xóa công ty thành công" });
=======
                return new JsonResult(new { success = true });
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
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
<<<<<<< HEAD
=======
            public int? CorporationID { get; set; }
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
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
<<<<<<< HEAD
=======
            public int? CorporationID { get; set; }
>>>>>>> 636d7a1c18d1f571743edee10ecfa2a89562f9c5
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