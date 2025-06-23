using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CMS.Pages.Manager
{
    public class CurrentPositionModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CurrentPositionModel(ApplicationDbContext context) => _context = context;

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

                var user = await _context.Users
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ErrorMessage = "User information not found in the system.";
                    return;
                }

                if (user.DepartmentID == null)
                {
                    ErrorMessage = "User is not assigned to any department.";
                    return;
                }

                Positions = await _context.Positions
                    .Where(p => p.DepartmentID == user.DepartmentID)
                    .Include(p => p.Department)
                    .ToListAsync();

                if (!Positions.Any())
                {
                    ErrorMessage = "No positions found for your department.";
                }

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (currentUser == null || currentUser.DepartmentID == null)
                {
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                var position = await _context.Positions
                    .Include(p => p.Department)
                    .FirstOrDefaultAsync(p => p.AutoID == id && p.DepartmentID == currentUser.DepartmentID);

                if (position == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy vị trí" });
                }

                return new JsonResult(new
                {
                    success = true,
                    position = new
                    {
                        autoID = position.AutoID,
                        positionName = position.PositionName,
                        positionSymbol = position.PositionSymbol,
                        descriptions = position.Descriptions,
                        isActive = position.IsActive ?? false,
                        departmentName = position.Department?.DepartmentName,
                        createBy = position.CreateBy,
                        createDate = position.CreateDate?.ToString("dd-MM-yyyy HH:mm:ss"),
                        lastModifiedBy = position.LastModifiedBy,
                        lastModifiedDate = position.LastModifiedDate?.ToString("dd-MM-yyyy HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        public class PositionInputModel
        {
            public int? AutoID { get; set; }
            [Required(ErrorMessage = "Tên vị trí là bắt buộc")]
            [StringLength(100, ErrorMessage = "Tên vị trí không được vượt quá 100 ký tự")]
            public string PositionName { get; set; }
            [Required(ErrorMessage = "Mã vị trí là bắt buộc")]
            [StringLength(50, ErrorMessage = "Mã vị trí không được vượt quá 50 ký tự")]
            public string PositionSymbol { get; set; }
            [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
            public string? Descriptions { get; set; }
            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnPostAddAsync([FromBody] PositionInputModel input)
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
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (currentUser == null || currentUser.DepartmentID == null)
                {
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                // Validate PositionSymbol uniqueness within the department
                if (await _context.Positions.AnyAsync(p => p.PositionSymbol == input.PositionSymbol && p.DepartmentID == currentUser.DepartmentID))
                {
                    return new JsonResult(new { success = false, message = "Mã vị trí đã tồn tại trong phòng ban" });
                }

                var newPosition = new Position
                {
                    PositionName = input.PositionName.Trim(),
                    PositionSymbol = input.PositionSymbol.Trim(),
                    Descriptions = input.Descriptions?.Trim(),
                    IsActive = input.IsActive,
                    DepartmentID = currentUser.DepartmentID,
                    CreateBy = currentUser.Id,
                    CreateDate = DateTime.Now
                };

                _context.Positions.Add(newPosition);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Thêm vị trí thành công" });
            }
            catch (Exception ex)
            {
                // Log the exception (implement logging as per your application's setup)
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromBody] PositionInputModel input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return new JsonResult(new { success = false, message = string.Join("; ", errors) });
                }

                if (!input.AutoID.HasValue)
                {
                    return new JsonResult(new { success = false, message = "ID vị trí không hợp lệ" });
                }

                var email = User.FindFirstValue(ClaimTypes.Email);
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (currentUser == null || currentUser.DepartmentID == null)
                {
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                var position = await _context.Positions
                    .FirstOrDefaultAsync(p => p.AutoID == input.AutoID && p.DepartmentID == currentUser.DepartmentID);

                if (position == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy vị trí hoặc không có quyền chỉnh sửa" });
                }

                // Validate PositionSymbol uniqueness within the department
                if (await _context.Positions.AnyAsync(p => p.PositionSymbol == input.PositionSymbol && p.DepartmentID == currentUser.DepartmentID && p.AutoID != input.AutoID))
                {
                    return new JsonResult(new { success = false, message = "Mã vị trí đã tồn tại trong phòng ban" });
                }

                position.PositionName = input.PositionName.Trim();
                position.PositionSymbol = input.PositionSymbol.Trim();
                position.Descriptions = input.Descriptions?.Trim();
                position.IsActive = input.IsActive;
                position.LastModifiedBy = currentUser.Id;
                position.LastModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Cập nhật vị trí thành công" });
            }
            catch (Exception ex)
            {
                // Log the exception (implement logging as per your application's setup)
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (currentUser == null || currentUser.DepartmentID == null)
                {
                    return new JsonResult(new { success = false, message = "Không thể xác định thông tin người dùng hiện tại" });
                }

                var position = await _context.Positions
                    .FirstOrDefaultAsync(p => p.AutoID == id && p.DepartmentID == currentUser.DepartmentID);

                if (position == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy vị trí hoặc không có quyền xóa" });
                }

                // Check if position is assigned to any users
                if (await _context.Users.AnyAsync(u => u.PositionID == id))
                {
                    return new JsonResult(new { success = false, message = "Không thể xóa vị trí vì đã được gán cho nhân viên" });
                }

                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Xóa vị trí thành công" });
            }
            catch (Exception ex)
            {
                // Log the exception (implement logging as per your application's setup)
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}