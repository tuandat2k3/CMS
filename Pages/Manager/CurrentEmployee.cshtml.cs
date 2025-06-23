using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CMS.Pages.Manager
{
    public class CurrentEmployeeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CurrentEmployeeModel(ApplicationDbContext context) => _context = context;

        public List<User> Employees { get; set; } = [];
        public List<Position> Positions { get; set; } = [];
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

                Employees = await _context.Users
                    .Where(u => u.DepartmentID == user.DepartmentID)
                    .Include(u => u.Department)
                    .Include(u => u.Position)
                    .Include(u => u.Branch)
                    .Include(u => u.Company)
                    .ToListAsync();

                Positions = await _context.Positions
                    .Where(p => p.DepartmentID == user.DepartmentID)
                    .ToListAsync();

                if (!Positions.Any())
                {
                    ErrorMessage = "No active positions found for your department.";
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetDetailsAsync(string id)
        {
            var employee = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .Include(u => u.Branch)
                .Include(u => u.Company)
                .Include(u => u.Corporation)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (employee == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy nhân viên" });
            }

            return new JsonResult(new
            {
                success = true,
                employee = new
                {
                    id = employee.Id,
                    fullName = employee.FullName,
                    staffCode = employee.StaffCode,
                    email = employee.Email,
                    phoneNumber = employee.PhoneNumber,
                    address = employee.Address,
                    station = employee.Station,
                    departmentName = employee.Department?.DepartmentName,
                    positionName = employee.Position?.PositionName,
                    branchName = employee.Branch?.BranchName,
                    companyName = employee.Company?.CompanyName,
                    corporationName = employee.Corporation?.CorporationName,
                    autoID = employee.PositionID
                }
            });
        }

        public class EmployeeInputModel
        {
            public string? Id { get; set; }
            [Required(ErrorMessage = "Họ tên là bắt buộc")]
            public string FullName { get; set; }
            [Required(ErrorMessage = "Mã nhân viên là bắt buộc")]
            public string StaffCode { get; set; }
            [Required(ErrorMessage = "Email là bắt buộc"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Address { get; set; }
            public string? Station { get; set; }
            [Required(ErrorMessage = "Vị trí là bắt buộc")]
            [Range(1, int.MaxValue, ErrorMessage = "Vị trí không hợp lệ")]
            public int PositionID { get; set; }
        }

        public async Task<IActionResult> OnPostAddAsync([FromBody] EmployeeInputModel input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return new JsonResult(new { success = false, message = string.Join("; ", errors) });
                }

                var email = User.FindFirstValue(ClaimTypes.Email);
                var currentUser = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.Branch)
                    .Include(u => u.Company)
                    .Include(u => u.Corporation)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (currentUser == null)
                {
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                // Validate PositionID
                var position = await _context.Positions
                    .FirstOrDefaultAsync(p => p.AutoID == input.PositionID && p.DepartmentID == currentUser.DepartmentID);
                if (position == null)
                {
                    return new JsonResult(new { success = false, message = "Vị trí không hợp lệ hoặc không hoạt động" });
                }

                // Validate StaffCode uniqueness
                if (await _context.Users.AnyAsync(u => u.StaffCode == input.StaffCode))
                {
                    return new JsonResult(new { success = false, message = "Mã nhân viên đã tồn tại" });
                }

                // Validate Email uniqueness
                if (await _context.Users.AnyAsync(u => u.Email == input.Email))
                {
                    return new JsonResult(new { success = false, message = "Email đã tồn tại" });
                }

                // Validate PhoneNumber format if provided
                if (!string.IsNullOrEmpty(input.PhoneNumber))
                {
                    var phoneRegex = new Regex(@"^\+?\d{10,15}$");
                    if (!phoneRegex.IsMatch(input.PhoneNumber))
                    {
                        return new JsonResult(new { success = false, message = "Số điện thoại không hợp lệ" });
                    }
                }

                var newEmployee = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FullName = input.FullName.Trim(),
                    StaffCode = input.StaffCode.Trim(),
                    Email = input.Email.Trim(),
                    PhoneNumber = input.PhoneNumber?.Trim(),
                    Address = input.Address?.Trim(),
                    Station = input.Station?.Trim(),
                    PositionID = input.PositionID,
                    DepartmentID = currentUser.DepartmentID,
                    BranchID = currentUser.BranchID,
                    CompanyID = currentUser.CompanyID,
                    CorporationID = currentUser.CorporationID,
                };

                _context.Users.Add(newEmployee);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Thêm nhân viên thành công" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromBody] EmployeeInputModel input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return new JsonResult(new { success = false, message = string.Join("; ", errors) });
                }

                var employee = await _context.Users
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == input.Id);

                if (employee == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy nhân viên" });
                }

                var email = User.FindFirstValue(ClaimTypes.Email);
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (currentUser == null || employee.DepartmentID != currentUser.DepartmentID)
                {
                    return new JsonResult(new { success = false, message = "Không có quyền chỉnh sửa nhân viên này" });
                }

                // Validate PositionID
                var position = await _context.Positions
                    .FirstOrDefaultAsync(p => p.AutoID == input.PositionID && p.DepartmentID == currentUser.DepartmentID);
                if (position == null)
                {
                    return new JsonResult(new { success = false, message = "Vị trí không hợp lệ hoặc không hoạt động" });
                }

                // Validate StaffCode uniqueness
                if (await _context.Users.AnyAsync(u => u.StaffCode == input.StaffCode && u.Id != input.Id))
                {
                    return new JsonResult(new { success = false, message = "Mã nhân viên đã tồn tại" });
                }

                // Validate Email uniqueness
                if (await _context.Users.AnyAsync(u => u.Email == input.Email && u.Id != input.Id))
                {
                    return new JsonResult(new { success = false, message = "Email đã tồn tại" });
                }

                // Validate PhoneNumber format if provided
                if (!string.IsNullOrEmpty(input.PhoneNumber))
                {
                    var phoneRegex = new Regex(@"^\+?\d{10,15}$");
                    if (!phoneRegex.IsMatch(input.PhoneNumber))
                    {
                        return new JsonResult(new { success = false, message = "Số điện thoại không hợp lệ" });
                    }
                }

                employee.FullName = input.FullName.Trim();
                employee.StaffCode = input.StaffCode.Trim();
                employee.Email = input.Email.Trim();
                employee.PhoneNumber = input.PhoneNumber?.Trim();
                employee.Address = input.Address?.Trim();
                employee.Station = input.Station?.Trim();
                employee.PositionID = input.PositionID;

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Cập nhật nhân viên thành công" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            try
            {
                var employee = await _context.Users
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (employee == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy nhân viên" });
                }

                var email = User.FindFirstValue(ClaimTypes.Email);
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (currentUser == null || employee.DepartmentID != currentUser.DepartmentID)
                {
                    return new JsonResult(new { success = false, message = "Không có quyền xóa nhân viên này" });
                }

                _context.Users.Remove(employee);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Xóa nhân viên thành công" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}