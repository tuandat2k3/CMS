using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Pages.Manager
{
    public class DepartmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DepartmentModel(ApplicationDbContext context) => _context = context;

        public Dictionary<Branch, List<Department>> DepartmentsByBranch { get; set; } = new();
        public List<Branch> Branches { get; set; } = new();
        public int? SelectedBranchId { get; set; }

        public async Task<IActionResult> OnGetAsync(string branchId)
        {
            // Lấy danh sách chi nhánh cho combobox
            Branches = await _context.Branches
                .Include(b => b.Company)
                .ThenInclude(c => c.Corporation)
                .ToListAsync();

            if (!Branches.Any() && branchId != "none")
            {
                ModelState.AddModelError("", "No branches found in the system.");
            }

            // Lấy danh sách phòng ban
            var departmentsQuery = _context.Departments
                .Include(d => d.Branch)
                .ThenInclude(b => b.Company)
                .ThenInclude(c => c.Corporation)
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

            return Page();
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Branch)
                .ThenInclude(b => b.Company)
                .ThenInclude(c => c.Corporation)
                .FirstOrDefaultAsync(d => d.AutoID == id);

            if (department == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                departmentName = department.DepartmentName,
                departmentSymbol = department.DepartmentSymbol,
                departmentDescription = department.DepartmentDescription,
                representative = department.Representative,
                isActive = department.IsActive,
                branchName = department.Branch?.BranchName,
                companyName = department.Branch?.Company?.CompanyName,
                corporationName = department.Branch?.Company?.Corporation?.CorporationName
            });
        }
    }
}