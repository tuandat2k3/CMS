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
    public class DepartmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DepartmentModel> _logger;
        public string? ErrorMessage { get; set; }

        public DepartmentModel(ApplicationDbContext context, ILogger<DepartmentModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Dictionary<Branch, List<Department>> DepartmentsByBranch { get; set; } = new();
        public List<Branch> Branches { get; set; } = new();
        public int? SelectedBranchId { get; set; }

        public async Task<IActionResult> OnGetAsync(string branchId)
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Không tìm thấy email người dùng đăng nhập");
                    ErrorMessage = "Không thể xác định thông tin người dùng hiện tại.";
                    return Page();
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser == null || currentUser.CompanyID == null)
                {
                    _logger.LogWarning("Không tìm thấy thông tin người dùng hoặc CompanyID: Email={Email}", email);
                    ErrorMessage = "Không tìm thấy thông tin người dùng hoặc CompanyID.";
                    return Page();
                }

                // Lấy danh sách chi nhánh cho combobox
                Branches = await _context.Branches
                    .Include(b => b.Company)
                    .ThenInclude(c => c.Corporation)
                    .Where(b => b.CompanyID == currentUser.CompanyID)
                    .ToListAsync();

                if (!Branches.Any() && branchId != "none")
                {
                    ModelState.AddModelError("", "Không tìm thấy chi nhánh nào trong hệ thống.");
                }

                // Lấy danh sách phòng ban
                var departmentsQuery = _context.Departments
                    .Include(d => d.Branch)
                    .ThenInclude(b => b.Company)
                    .ThenInclude(c => c.Corporation)
                    .Where(d => d.Branch == null || d.Branch.CompanyID == currentUser.CompanyID)
                    .AsQueryable();

                // Lọc theo chi nhánh nếu có branchId
                if (branchId == "none")
                {
                    departmentsQuery = departmentsQuery.Where(d => d.BranchID == null);
                }
                else if (int.TryParse(branchId, out int parsedBranchId) && parsedBranchId != 0)
                {
                    departmentsQuery = departmentsQuery.Where(d => d.BranchID == parsedBranchId);
                    SelectedBranchId = parsedBranchId;
                }
                else
                {
                    SelectedBranchId = null; // Mặc định là "All Branches"
                }

                var departments = await departmentsQuery.ToListAsync();

                // Nhóm phòng ban theo chi nhánh
                DepartmentsByBranch = departments
                    .GroupBy(d => d.Branch ?? new Branch { BranchName = "No Branch", AutoID = 0 })
                    .ToDictionary(g => g.Key, g => g.ToList());

                _logger.LogInformation("Đã tải {Count} phòng ban", departments.Count);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách phòng ban");
                ErrorMessage = $"Đã xảy ra lỗi khi tải dữ liệu: {ex.Message}";
                return Page();
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

                var department = await _context.Departments
                    .Include(d => d.Branch)
                    .ThenInclude(b => b.Company)
                    .ThenInclude(c => c.Corporation)
                    .FirstOrDefaultAsync(d => d.AutoID == id && (d.Branch == null || d.Branch.CompanyID == currentUser.CompanyID));

                if (department == null)
                {
                    _logger.LogWarning("Không tìm thấy phòng ban hoặc không có quyền truy cập: Id={Id}", id);
                    return new JsonResult(new { success = false, message = "Không tìm thấy phòng ban hoặc không có quyền truy cập" });
                }

                _logger.LogInformation("Đã lấy chi tiết phòng ban: Id={Id}", id);
                return new JsonResult(new
                {
                    success = true,
                    department = new
                    {
                        autoID = department.AutoID,
                        departmentName = department.DepartmentName,
                        departmentSymbol = department.DepartmentSymbol,
                        departmentDescription = department.DepartmentDescription,
                        representative = department.Representative,
                        isActive = department.IsActive ?? false,
                        branchName = department.Branch?.BranchName,
                        companyName = department.Branch?.Company?.CompanyName,
                        corporationName = department.Branch?.Company?.Corporation?.CorporationName,
                        createBy = department.CreateBy,
                        createDate = department.CreateDate?.ToString("dd/MM/yyyy HH:mm:ss"),
                        lastModifiedBy = department.LastModifiedBy,
                        lastModifiedDate = department.LastModifiedDate?.ToString("dd/MM/yyyy HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết phòng ban: Id={Id}", id);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải chi tiết: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostAddDepartmentAsync([FromBody] AddDepartmentModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi thêm phòng ban: {Errors}", string.Join("; ", errors));
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

                // Validate BranchID if provided
                if (model.BranchID.HasValue)
                {
                    var branch = await _context.Branches
                        .FirstOrDefaultAsync(b => b.AutoID == model.BranchID && b.CompanyID == currentUser.CompanyID);
                    if (branch == null)
                    {
                        _logger.LogWarning("Chi nhánh không hợp lệ hoặc không thuộc công ty của người dùng: BranchID={BranchID}", model.BranchID);
                        return new JsonResult(new { success = false, message = "Chi nhánh không hợp lệ hoặc không thuộc công ty của bạn" });
                    }
                }

                // Validate DepartmentSymbol uniqueness within the branch
                if (!string.IsNullOrEmpty(model.DepartmentSymbol) && await _context.Departments
                    .AnyAsync(d => d.DepartmentSymbol == model.DepartmentSymbol && d.BranchID == model.BranchID))
                {
                    _logger.LogWarning("Mã phòng ban đã tồn tại trong chi nhánh: {DepartmentSymbol}", model.DepartmentSymbol);
                    return new JsonResult(new { success = false, message = "Mã phòng ban đã tồn tại trong chi nhánh" });
                }

                _logger.LogInformation("Thêm phòng ban mới: {DepartmentName}", model.DepartmentName);
                var department = new Department
                {
                    BranchID = model.BranchID,
                    DepartmentName = model.DepartmentName?.Trim(),
                    DepartmentSymbol = model.DepartmentSymbol?.Trim(),
                    DepartmentDescription = model.DepartmentDescription?.Trim(),
                    Representative = model.Representative?.Trim(),
                    IsActive = model.IsActive,
                    CreateBy = currentUser.Id,
                    CreateDate = DateTime.Now,
                    LastModifiedBy = currentUser.Id,
                    LastModifiedDate = DateTime.Now
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Thêm phòng ban thành công: Id={Id}", department.AutoID);
                TempData["Success"] = "Thêm phòng ban thành công";
                return new JsonResult(new { success = true, message = "Thêm phòng ban thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm phòng ban: {DepartmentName}", model.DepartmentName);
                TempData["Error"] = $"Lỗi khi thêm phòng ban: {ex.Message}";
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm phòng ban: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostUpdateDepartmentAsync([FromBody] UpdateDepartmentModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi cập nhật phòng ban: {Errors}", string.Join("; ", errors));
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

                var department = await _context.Departments
                    .Include(d => d.Branch)
                    .FirstOrDefaultAsync(d => d.AutoID == model.AutoID && (d.Branch == null || d.Branch.CompanyID == currentUser.CompanyID));
                if (department == null)
                {
                    _logger.LogWarning("Không tìm thấy phòng ban hoặc không có quyền cập nhật: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không tìm thấy phòng ban hoặc không có quyền cập nhật" });
                }

                // Validate DepartmentSymbol uniqueness within the branch
                if (!string.IsNullOrEmpty(model.DepartmentSymbol) && await _context.Departments
                    .AnyAsync(d => d.DepartmentSymbol == model.DepartmentSymbol && d.BranchID == department.BranchID && d.AutoID != model.AutoID))
                {
                    _logger.LogWarning("Mã phòng ban đã tồn tại trong chi nhánh: {DepartmentSymbol}", model.DepartmentSymbol);
                    return new JsonResult(new { success = false, message = "Mã phòng ban đã tồn tại trong chi nhánh" });
                }

                department.DepartmentName = model.DepartmentName?.Trim();
                department.DepartmentSymbol = model.DepartmentSymbol?.Trim();
                department.DepartmentDescription = model.DepartmentDescription?.Trim();
                department.Representative = model.Representative?.Trim();
                department.IsActive = model.IsActive;
                department.LastModifiedBy = currentUser.Id;
                department.LastModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cập nhật phòng ban thành công: Id={Id}", model.AutoID);
                TempData["Success"] = "Cập nhật phòng ban thành công";
                return new JsonResult(new { success = true, message = "Cập nhật phòng ban thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật phòng ban: Id={Id}", model.AutoID);
                TempData["Error"] = $"Lỗi khi cập nhật phòng ban: {ex.Message}";
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật phòng ban: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostDeleteDepartmentAsync([FromBody] DeleteDepartmentModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Dữ liệu không hợp lệ khi xóa phòng ban: {Errors}", string.Join("; ", errors));
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

                var department = await _context.Departments
                    .Include(d => d.Branch)
                    .FirstOrDefaultAsync(d => d.AutoID == model.AutoID && (d.Branch == null || d.Branch.CompanyID == currentUser.CompanyID));
                if (department == null)
                {
                    _logger.LogWarning("Không tìm thấy phòng ban hoặc không có quyền xóa: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không tìm thấy phòng ban hoặc không có quyền xóa" });
                }

                // Check for dependencies
                if (await _context.Positions.AnyAsync(p => p.DepartmentID == model.AutoID) ||
                    await _context.Users.AnyAsync(u => u.DepartmentID == model.AutoID) ||
                    await _context.Contracts.AnyAsync(c => c.DepartmentID == model.AutoID) ||
                    await _context.Files.AnyAsync(f => f.DepartmentID == model.AutoID) ||
                    await _context.PermissionCompanies.AnyAsync(p => p.DepartmentID == model.AutoID))
                {
                    _logger.LogWarning("Không thể xóa phòng ban vì đã được liên kết: Id={Id}", model.AutoID);
                    return new JsonResult(new { success = false, message = "Không thể xóa phòng ban vì đã được liên kết với vị trí, người dùng, hợp đồng, tệp hoặc quyền" });
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Xóa phòng ban thành công: Id={Id}", model.AutoID);
                TempData["Success"] = "Xóa phòng ban thành công";
                return new JsonResult(new { success = true, message = "Xóa phòng ban thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa phòng ban: Id={Id}", model.AutoID);
                TempData["Error"] = $"Lỗi khi xóa phòng ban: {ex.Message}";
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa phòng ban: {ex.Message}" });
            }
        }

        public class AddDepartmentModel
        {
            [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
            public string DepartmentName { get; set; }
            public string? DepartmentSymbol { get; set; }
            public string? DepartmentDescription { get; set; }
            public string? Representative { get; set; }
            [Required]
            public bool IsActive { get; set; }
            public int? BranchID { get; set; }
        }

        public class UpdateDepartmentModel
        {
            [Required]
            public int AutoID { get; set; }
            [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
            public string DepartmentName { get; set; }
            public string? DepartmentSymbol { get; set; }
            public string? DepartmentDescription { get; set; }
            public string? Representative { get; set; }
            [Required]
            public bool IsActive { get; set; }
        }

        public class DeleteDepartmentModel
        {
            [Required]
            public int AutoID { get; set; }
        }
    }
}
