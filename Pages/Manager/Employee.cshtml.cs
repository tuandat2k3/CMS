using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Pages.Manager
{
    public class EmployeeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EmployeeModel(ApplicationDbContext context) => _context = context;

        public List<User> Employees { get; set; } = [];

        public async Task OnGetAsync()
        {
            Employees = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .Include(u => u.Branch)
                .Include(u => u.Company)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetDetailsAsync(string id)
        {
            var employee = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .Include(u => u.Branch)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                employee.Id,
                employee.FullName,
                employee.StaffCode,
                employee.Email,
                employee.PhoneNumber,
                employee.Address,
                employee.Station,
                DepartmentName = employee.Department?.DepartmentName,
                PositionName = employee.Position?.PositionName,
                BranchName = employee.Branch?.BranchName,
                CompanyName = employee.Company?.CompanyName
            });
        }

        //them 

        //sua

        //xoa

        //Thang or giang cap(dua vao rolename)


    }
}