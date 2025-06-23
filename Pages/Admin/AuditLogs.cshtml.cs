using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.Data;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CMS.Pages.AuditLogs
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AuditLogViewModel> AuditLogs { get; set; } = new List<AuditLogViewModel>();
        public List<User> Users { get; set; } = new List<User>();

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TableName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Action { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Users = await _context.Users.ToListAsync();

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(UserId))
                query = query.Where(a => a.UserID == UserId);

            if (!string.IsNullOrEmpty(TableName))
                query = query.Where(a => a.Tables == TableName);

            if (!string.IsNullOrEmpty(Action))
                query = query.Where(a => a.Action == Action);

            if (FromDate.HasValue)
                query = query.Where(a => a.CreateDate >= FromDate.Value);

            if (ToDate.HasValue)
                query = query.Where(a => a.CreateDate <= ToDate.Value.Date.AddDays(1).AddTicks(-1));

            int totalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);

            var logs = await query
                .OrderByDescending(a => a.CreateDate)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var userDict = Users.ToDictionary(u => u.Id, u => u.FullName);

            AuditLogs = logs.Select(log => new AuditLogViewModel
            {
                AutoID = log.AutoID,
                UserID = log.UserID,
                UserFullName = userDict.ContainsKey(log.UserID) ? userDict[log.UserID] : "N/A",
                Tables = log.Tables,
                Action = log.Action,
                Note = log.Note,
                CreateDate = log.CreateDate
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnGetDetailsAsync(int autoId)
        {
            var log = await _context.AuditLogs.FirstOrDefaultAsync(a => a.AutoID == autoId);
            if (log == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy dữ liệu." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == log.UserID);

            return new JsonResult(new
            {
                success = true,
                autoID = log.AutoID,
                userFullName = user?.FullName ?? "N/A",
                tables = log.Tables,
                action = log.Action,
                note = log.Note,
                details = log.Data,
                createDate = log.CreateDate?.ToString("dd/MM/yyyy HH:mm:ss")
            });
        }

        public class AuditLogViewModel
        {
            public int AutoID { get; set; }
            public string? UserID { get; set; }
            public string? UserFullName { get; set; }
            public string? Tables { get; set; }
            public string? Action { get; set; }
            public string? Note { get; set; }
            public DateTime? CreateDate { get; set; }
        }
    }
}