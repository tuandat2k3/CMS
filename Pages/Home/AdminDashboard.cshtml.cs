using Microsoft.EntityFrameworkCore;
using CMS.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CMS.Data;

namespace CMS.Pages.Home
{
    public class AdminDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int UserCount { get; set; }
        public int ActiveContractCount { get; set; }

        public async Task OnGetAsync()
        {
            UserCount = await _context.Users.CountAsync();
            ActiveContractCount = await _context.Contracts.CountAsync(c => c.IsActive);
        }
    }
}