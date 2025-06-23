using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CMS.Pages.Account
{
    public class ExternalLoginCallbackModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AdminAccounts _adminAccounts;

        public ExternalLoginCallbackModel(ApplicationDbContext context, IOptions<AdminAccounts> adminAccounts)
        {
            _context = context;
            _adminAccounts = adminAccounts.Value;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var result = await HttpContext.AuthenticateAsync();

            if (!result.Succeeded)
                return RedirectToPage("/Error");

            var emailClaim = result.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
                return RedirectToPage("/Error");

            string email = emailClaim.Value;
            string roleName = "Quản lý"; // Vai trò mặc định

            // Kiểm tra xem email có trong danh sách admin không
            bool isAdmin = _adminAccounts?.Emails?.Contains(email, StringComparer.OrdinalIgnoreCase) == true;
            if (isAdmin)
            {
                roleName = "Admin";
            }

            // Kiểm tra cơ sở dữ liệu
            bool dbAvailable = _context != null && _context.Database.CanConnect();
            if (dbAvailable)
            {
                // Tìm user trong DB
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // Tạo user mới
                    user = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = email,
                        Address = "null",
                        FullName = "null",
                        StaffCode = "null",
                        Station = "null"


                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Nếu không phải admin, kiểm tra vai trò từ DB
                if (!isAdmin)
                {
                    var dbRole = await (from ur in _context.UserRoles
                                        join r in _context.Roles on ur.RolesID equals r.RolesID
                                        where ur.UserId == user.Id
                                        select r.RoleName).FirstOrDefaultAsync();
                    if (dbRole != null)
                    {
                        roleName = dbRole;
                    }
                }

                // Nếu là admin, đảm bảo vai trò Admin được lưu vào DB
                if (isAdmin)
                {
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                    if (adminRole == null)
                    {
                        adminRole = new Role
                        {
                            RolesID = Guid.NewGuid(),
                            RoleName = "Admin"
                        };
                        _context.Roles.Add(adminRole);
                        await _context.SaveChangesAsync();
                    }

                    var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RolesID == adminRole.RolesID);
                    if (userRole == null)
                    {
                        userRole = new UserRole
                        {
                            UserId = user.Id,
                            RolesID = adminRole.RolesID
                        };
                        _context.UserRoles.Add(userRole);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // Gán claims
            var identity = new ClaimsIdentity(result.Principal.Identity);
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));

            var principal = new ClaimsPrincipal(identity);

            // Đăng nhập với cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Chuyển hướng theo vai trò
            return roleName switch
            {
                "Admin" => RedirectToPage("/Home/AdminDashboard"),
                "Giám Đốc" => RedirectToPage("/Home/AdminDashboard"),
                "Quản lý" => RedirectToPage("/Home/ManagerDashboard"),
                _ => RedirectToPage("/Home/UserDashboard")
            };
        }
    }

}