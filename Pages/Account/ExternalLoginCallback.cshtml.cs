using CMS.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CMS.Pages.Account
{
    public class ExternalLoginCallbackModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExternalLoginCallbackModel(ApplicationDbContext context)
        {
            _context = context;
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

            // Tìm user trong DB theo email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return RedirectToPage("/Error");

            // Truy vấn role đầu tiên từ DB
            var roleName = await (from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RolesID equals r.RolesID
                                  where ur.UserId == user.Id
                                  select r.RoleName).FirstOrDefaultAsync();

            if (roleName == null)
                roleName = "Viewer"; // mặc định nếu không có role

            // Gán claim mới
            var identity = new ClaimsIdentity(result.Principal.Identity, result.Principal.Claims);
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));

            var principal = new ClaimsPrincipal(identity);

            // Đăng nhập lại với Cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Chuyển hướng theo role
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
