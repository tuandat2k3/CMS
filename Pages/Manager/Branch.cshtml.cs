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
    public class BranchModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BranchModel> _logger;
        public string? ErrorMessage { get; set; }

        public BranchModel(ApplicationDbContext context, ILogger<BranchModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Branch> Branches { get; set; } = new();
        public bool IsLoaded { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Không tìm thấy email người dùng đăng nhập");
                    ErrorMessage = "Không thể xác định thông tin người dùng hiện tại.";
                    return;
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser == null || currentUser.CompanyID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CompanyID: Email={Email}", email);
                    ErrorMessage = "Không tìm thấy thông tin người dùng hoặc CompanyID.";
                    return;
                }

                Branches = await _context.Branches
                    .Where(b => b.CompanyID == currentUser.CompanyID)
                    .Include(b => b.Company)
                    .OrderBy(b => b.BranchName)
                    .ToListAsync();
                _logger.LogInformation("Đã tải {Count} chi nhánh", Branches.Count);
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách chi nhánh");
                ErrorMessage = $"Đã xảy ra lỗi khi tải dữ liệu: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Không tìm thấy email người dùng đăng nhập");
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser == null || currentUser.CompanyID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CompanyID: Email={Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng hoặc CompanyID" });
                }

                var branch = await _context.Branches
                    .Include(b => b.Company)
                    .FirstOrDefaultAsync(b => b.AutoID == id && b.CompanyID == currentUser.CompanyID);

                if (branch == null)
                {
                    _logger.LogWarning("Không tìm thấy chi nhánh hoặc không có quyền truy cập: Id={Id}", id);
                    return new JsonResult(new { success = false, message = "Không tìm thấy chi nhánh hoặc không có quyền truy cập" });
                }

                _logger.LogInformation("Đã lấy chi tiết chi nhánh: Id={Id}", id);
                return new JsonResult(new
                {
                    success = true,
                    branch = new
                    {
                        autoID = branch.AutoID,
                        branchName = branch.BranchName,
                        branchSymbol = branch.BranchSymbol,
                        address = branch.Address,
                        city = branch.City,
                        country = branch.Country,
                        phone = branch.Phone,
                        homePhone = branch.HomePhone,
                        representative = branch.Representative,
                        isActive = branch.IsActive ?? false,
                        companyName = branch.Company?.CompanyName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết chi nhánh: Id={Id}", id);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải chi tiết: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostAddBranchAsync([FromBody] AddBranchModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi thêm chi nhánh: {Errors}", string.Join("; ", errors));
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
                if (currentUser == null || currentUser.CompanyID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CompanyID: Email={Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng hoặc CompanyID" });
                }

                // Validate BranchSymbol uniqueness within the company
                if (!string.IsNullOrEmpty(model.BranchSymbol) && await _context.Branches
                    .AnyAsync(b => b.BranchSymbol == model.BranchSymbol && b.CompanyID == currentUser.CompanyID))
                {
                    _logger.LogWarning("Mã chi nhánh đã tồn tại: {BranchSymbol}", model.BranchSymbol);
                    return new JsonResult(new { success = false, message = "Mã chi nhánh đã tồn tại trong công ty" });
                }

                _logger.LogInformation("Thêm chi nhánh mới: {BranchName}", model.BranchName);
                var branch = new Branch
                {
                    CompanyID = currentUser.CompanyID,
                    BranchName = model.BranchName?.Trim(),
                    BranchSymbol = model.BranchSymbol?.Trim(),
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
                    LastModifiedDate = DateTime.Now
                };

                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Thêm chi nhánh thành công: Id={Id}", branch.AutoID);
                TempData["Success"] = "Thêm chi nhánh thành công";
                return new JsonResult(new { success = true, message = "Thêm chi nhánh thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm chi nhánh: {BranchName}", model.BranchName);
                TempData["Error"] = $"Lỗi khi thêm chi nhánh: {ex.Message}";
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm chi nhánh: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostUpdateBranchAsync([FromBody] UpdateBranchModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi cập nhật chi nhánh: {Errors}", string.Join("; ", errors));
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
                if (currentUser == null || currentUser.CompanyID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CompanyID: Email={Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng hoặc CompanyID" });
                }

                var branch = await _context.Branches
                    .FirstOrDefaultAsync(b => b.AutoID == model.AutoID && b.CompanyID == currentUser.CompanyID);
                if (branch == null)
                {
                    _logger.LogWarning("Không tìm thấy chi nhánh hoặc không có quyền cập nhật: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không tìm thấy chi nhánh hoặc không có quyền cập nhật" });
                }

                // Validate BranchSymbol uniqueness within the company
                if (!string.IsNullOrEmpty(model.BranchSymbol) && await _context.Branches
                    .AnyAsync(b => b.BranchSymbol == model.BranchSymbol && b.CompanyID == currentUser.CompanyID && b.AutoID != model.AutoID))
                {
                    _logger.LogWarning("Mã chi nhánh đã tồn tại: {BranchSymbol}", model.BranchSymbol);
                    return new JsonResult(new { success = false, message = "Mã chi nhánh đã tồn tại trong công ty" });
                }

                branch.BranchName = model.BranchName?.Trim();
                branch.BranchSymbol = model.BranchSymbol?.Trim();
                branch.Address = model.Address?.Trim();
                branch.City = model.City?.Trim();
                branch.Country = model.Country?.Trim();
                branch.Phone = model.Phone?.Trim();
                branch.HomePhone = model.HomePhone?.Trim();
                branch.Representative = model.Representative?.Trim();
                branch.IsActive = model.IsActive;
                branch.LastModifiedBy = currentUser.Id;
                branch.LastModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cập nhật chi nhánh thành công: Id={Id}", model.AutoID);
                TempData["Success"] = "Cập nhật chi nhánh thành công";
                return new JsonResult(new { success = true, message = "Cập nhật chi nhánh thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật chi nhánh: Id={Id}", model.AutoID);
                TempData["Error"] = $"Lỗi khi cập nhật chi nhánh: {ex.Message}";
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật chi nhánh: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostDeleteBranchAsync([FromBody] DeleteBranchModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi xóa chi nhánh: {Errors}", string.Join("; ", errors));
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
                if (currentUser == null || currentUser.CompanyID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CompanyID: Email={Email}", email);
                    return new JsonResult(new { success = false, message = "Không tìm thấy thông tin người dùng hoặc CompanyID" });
                }

                var branch = await _context.Branches
                    .FirstOrDefaultAsync(b => b.AutoID == model.AutoID && b.CompanyID == currentUser.CompanyID);
                if (branch == null)
                {
                    _logger.LogWarning("Không tìm thấy chi nhánh hoặc không có quyền xóa: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không tìm thấy chi nhánh hoặc không có quyền xóa" });
                }

                // Check for dependencies
                if (await _context.Departments.AnyAsync(d => d.BranchID == model.AutoID) ||
                    await _context.Users.AnyAsync(u => u.BranchID == model.AutoID) ||
                    await _context.Contracts.AnyAsync(c => c.BranchID == model.AutoID) ||
                    await _context.Files.AnyAsync(f => f.BranchID == model.AutoID) ||
                    await _context.PermissionCompanies.AnyAsync(p => p.BranchID == model.AutoID))
                {
                    _logger.LogWarning("Không thể xóa chi nhánh vì đã được liên kết: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không thể xóa chi nhánh vì đã được liên kết với phòng ban, người dùng, hợp đồng, tệp hoặc quyền" });
                }

                _context.Branches.Remove(branch);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Xóa chi nhánh thành công: Id={Id}", model.AutoID);
                TempData["Success"] = "Xóa chi nhánh thành công";
                return new JsonResult(new { success = true, message = "Xóa chi nhánh thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa chi nhánh: Id={Id}", model.AutoID);
                TempData["Error"] = $"Lỗi khi xóa chi nhánh: {ex.Message}";
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa chi nhánh: {ex.Message}" });
            }
        }

        public class AddBranchModel
        {
            [Required(ErrorMessage = "Tên chi nhánh là bắt buộc")]
            public string BranchName { get; set; }
            public string? BranchSymbol { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
            public string? Phone { get; set; }
            public string? HomePhone { get; set; }
            public string? Representative { get; set; }
            [Required]
            public bool IsActive { get; set; }
        }

        public class UpdateBranchModel
        {
            [Required]
            public int AutoID { get; set; }
            [Required(ErrorMessage = "Tên chi nhánh là bắt buộc")]
            public string BranchName { get; set; }
            public string? BranchSymbol { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
            public string? Phone { get; set; }
            public string? HomePhone { get; set; }
            public string? Representative { get; set; }
            [Required]
            public bool IsActive { get; set; }
        }

        public class DeleteBranchModel
        {
            [Required]
            public int AutoID { get; set; }
        }
    }
}