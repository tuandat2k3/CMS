using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CMS.Models;
using CMS.Data;

namespace CMS.Pages.Home
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProfileModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; set; }
        public string Layout { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Lấy email từ claims
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Account/Login");
            }

            // Tìm User và Role của User
            CurrentUser = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Branch)
                .Include(u => u.Company)
                .Include(u => u.Corporation)
                .Include(u => u.Department)
                .Include(u => u.Position)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (CurrentUser == null)
            {
                return NotFound();
            }

            // Lấy RoleName đầu tiên
            var roleName = CurrentUser.UserRoles?.FirstOrDefault()?.Role?.RoleName;

            // Gán Layout dựa vào Role
            if (!string.IsNullOrEmpty(roleName))
            {
                if (roleName.Equals("admin", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Giám đốc", StringComparison.OrdinalIgnoreCase))
                {
                    Layout = "_AdminLayout";
                }
                else if (roleName.Equals("Quản lý", StringComparison.OrdinalIgnoreCase))
                {
                    Layout = "_ManagerLayout";
                }
                else
                {
                    Layout = "_Layout";
                }
            }
            else
            {
                Layout = "_Layout";
            }

            return Page();
        }
    }
}
