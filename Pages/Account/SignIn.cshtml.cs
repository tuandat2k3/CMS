using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CMS.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CMS.Pages.Account
{
    public class SignInModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public SignInModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
            [MaxLength(256)]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLocalLoginAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Find user by username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == Input.UserName);

            if (user == null || !VerifyPassword(Input.Password, user.Password))
            {
                ViewData["ErrorMessage"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return Page();
            }

            // Get user's role
            var roleName = await (from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RolesID equals r.RolesID
                                  where ur.UserId == user.Id
                                  select r.RoleName).FirstOrDefaultAsync();

            if (roleName == null)
            {
                roleName = "Viewer"; // Default role if none assigned
            }

            // Create claims
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false // Set to true if you want "Remember Me" functionality
            };

            // Sign in user
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect based on role
            return roleName switch
            {
                "Admin" => RedirectToPage("/Home/AdminDashboard"),
                "Giám Đốc" => RedirectToPage("/Home/AdminDashboard"),
                "Quản lý" => RedirectToPage("/Home/ManagerDashboard"),
                _ => RedirectToPage("/Index")
            };
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            // Implement your password verification logic here
            // For production, use proper password hashing (e.g., BCrypt, Argon2)
            // This is a placeholder - replace with secure implementation
            return inputPassword == storedPassword;
        }
    }
}
