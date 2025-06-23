using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Text.Json;
using System.Security.Claims;

namespace CMS.Pages.Contract
{
    public class ChartModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ChartModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<StatusStats> StatusStatistics { get; set; }
        public List<QuarterStats> QuarterStatistics { get; set; }
        public List<MonthlyRevenueStats> MonthlyRevenueStatistics { get; set; }
        public List<QuarterlyRevenueStats> QuarterlyRevenueStatistics { get; set; }
        public List<ContractTypeStats> ContractTypeStatistics { get; set; }
        public List<PartnerStats> PartnerStatistics { get; set; }

        public async Task OnGetAsync()
        {
            var contracts = await _context.Contracts
                .Include(c => c.Company)
                .Include(c => c.Branch)
                .Include(c => c.Department)
                .Include(c => c.Partner)
                .Where(c => !c.IsDeleted && c.IsActive)
                .ToListAsync();

            // Thống kê theo trạng thái
            StatusStatistics = contracts
                .GroupBy(c => c.ContractStatus)
                .Select(g => new StatusStats
                {
                    ContractStatus = g.Key,
                    ContractCount = g.Count()
                })
                .OrderBy(s => s.ContractStatus)
                .ToList();

            // Thống kê theo quý
            QuarterStatistics = contracts
                .GroupBy(c => new
                {
                    Quarter = (c.StartDate.Value.Month - 1) / 3 + 1,
                    c.ContractYear
                })
                .Select(g => new QuarterStats
                {
                    Quarter = g.Key.Quarter,
                    ContractYear = g.Key.ContractYear,
                    ContractCount = g.Count()
                })
                .OrderBy(s => s.ContractYear)
                .ThenBy(s => s.Quarter)
                .ToList();

            // Thống kê doanh thu theo tháng
            MonthlyRevenueStatistics = contracts
                .Where(c => c.ContractValue.HasValue)
                .GroupBy(c => new
                {
                    c.StartDate.Value.Year,
                    c.StartDate.Value.Month
                })
                .Select(g => new MonthlyRevenueStats
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(c => c.ContractValue.Value)
                })
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToList();

            // Thống kê doanh thu theo quý
            QuarterlyRevenueStatistics = contracts
                .Where(c => c.ContractValue.HasValue)
                .GroupBy(c => new
                {
                    Quarter = (c.StartDate.Value.Month - 1) / 3 + 1,
                    c.ContractYear
                })
                .Select(g => new QuarterlyRevenueStats
                {
                    Quarter = g.Key.Quarter,
                    ContractYear = g.Key.ContractYear,
                    Revenue = g.Sum(c => c.ContractValue.Value)
                })
                .OrderBy(s => s.ContractYear)
                .ThenBy(s => s.Quarter)
                .ToList();

            // Thống kê doanh thu theo loại hợp đồng
            ContractTypeStatistics = contracts
                .Where(c => c.ContractValue.HasValue)
                .GroupBy(c => c.ContractType)
                .Select(g => new ContractTypeStats
                {
                    ContractType = g.Key,
                    Revenue = g.Sum(c => c.ContractValue.Value)
                })
                .OrderBy(s => s.ContractType)
                .ToList();

            // Thống kê số lượng hợp đồng theo đối tác
            PartnerStatistics = contracts
                .GroupBy(c => c.Partner.CompanyName)
                .Select(g => new PartnerStats
                {
                    PartnerName = g.Key ?? "Unknown",
                    ContractCount = g.Count()
                })
                .OrderBy(s => s.PartnerName)
                .ToList();
        }

        public async Task<IActionResult> OnGetDownloadExcelAsync()
        {
            await OnGetAsync();

            ExcelPackage.License.SetNonCommercialPersonal("CMS");

            try
            {
                using (var package = new ExcelPackage())
                {
                    // Status Statistics
                    var statusSheet = package.Workbook.Worksheets.Add("Status Statistics");
                    statusSheet.Cells[1, 1].Value = "Contract Status";
                    statusSheet.Cells[1, 2].Value = "Contract Count";

                    for (int i = 0; i < StatusStatistics.Count; i++)
                    {
                        statusSheet.Cells[i + 2, 1].Value = StatusStatistics[i].ContractStatus;
                        statusSheet.Cells[i + 2, 2].Value = StatusStatistics[i].ContractCount;
                    }

                    int statusTotalRow = StatusStatistics.Count + 2;
                    statusSheet.Cells[statusTotalRow, 1].Value = "Total";
                    statusSheet.Cells[statusTotalRow, 2].Value = StatusStatistics.Sum(s => s.ContractCount);

                    var statusRange = statusSheet.Cells[1, 1, statusTotalRow, 2];
                    statusRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    statusRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    statusRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    statusRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    statusRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    statusRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    // Quarter Statistics
                    var quarterSheet = package.Workbook.Worksheets.Add("Quarter Statistics");
                    quarterSheet.Cells[1, 1].Value = "Year";
                    quarterSheet.Cells[1, 2].Value = "Quarter";
                    quarterSheet.Cells[1, 3].Value = "Contract Count";
                    quarterSheet.Cells[1, 4].Value = "Total";

                    var groupedByYear = QuarterStatistics.GroupBy(q => q.ContractYear).OrderBy(g => g.Key);
                    int currentRow = 2;
                    int totalContracts = 0;

                    foreach (var yearGroup in groupedByYear)
                    {
                        var yearData = yearGroup.OrderBy(q => q.Quarter).ToList();
                        int yearStartRow = currentRow;
                        int yearTotal = yearData.Sum(q => q.ContractCount);
                        totalContracts += yearTotal;

                        foreach (var quarter in yearData)
                        {
                            quarterSheet.Cells[currentRow, 2].Value = quarter.Quarter;
                            quarterSheet.Cells[currentRow, 3].Value = quarter.ContractCount;
                            currentRow++;
                        }

                        if (yearData.Count > 0)
                        {
                            quarterSheet.Cells[yearStartRow, 4, currentRow - 1, 4].Merge = true;
                            quarterSheet.Cells[yearStartRow, 4].Value = yearTotal;
                        }

                        quarterSheet.Cells[yearStartRow, 1].Value = yearGroup.Key;
                        if (yearData.Count > 1)
                        {
                            quarterSheet.Cells[yearStartRow, 1, currentRow - 1, 1].Merge = true;
                        }
                    }

                    quarterSheet.Cells[currentRow, 3].Value = "Grand Total";
                    quarterSheet.Cells[currentRow, 4].Value = totalContracts;

                    var quarterRange = quarterSheet.Cells[1, 1, currentRow, 4];
                    quarterRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    quarterRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    quarterRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    quarterRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    quarterRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    quarterRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    // Monthly Revenue Statistics
                    var monthlyRevenueSheet = package.Workbook.Worksheets.Add("Monthly Revenue Statistics");
                    monthlyRevenueSheet.Cells[1, 1].Value = "Year";
                    monthlyRevenueSheet.Cells[1, 2].Value = "Month";
                    monthlyRevenueSheet.Cells[1, 3].Value = "Revenue";

                    for (int i = 0; i < MonthlyRevenueStatistics.Count; i++)
                    {
                        monthlyRevenueSheet.Cells[i + 2, 1].Value = MonthlyRevenueStatistics[i].Year;
                        monthlyRevenueSheet.Cells[i + 2, 2].Value = MonthlyRevenueStatistics[i].Month;
                        monthlyRevenueSheet.Cells[i + 2, 3].Value = MonthlyRevenueStatistics[i].Revenue;
                    }

                    var monthlyRevenueRange = monthlyRevenueSheet.Cells[1, 1, MonthlyRevenueStatistics.Count + 2, 3];
                    monthlyRevenueRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    monthlyRevenueRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    monthlyRevenueRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    monthlyRevenueRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    monthlyRevenueRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    monthlyRevenueRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    // Quarterly Revenue Statistics
                    var quarterlyRevenueSheet = package.Workbook.Worksheets.Add("Quarterly Revenue Statistics");
                    quarterlyRevenueSheet.Cells[1, 1].Value = "Year";
                    quarterlyRevenueSheet.Cells[1, 2].Value = "Quarter";
                    quarterlyRevenueSheet.Cells[1, 3].Value = "Revenue";

                    for (int i = 0; i < QuarterlyRevenueStatistics.Count; i++)
                    {
                        quarterlyRevenueSheet.Cells[i + 2, 1].Value = QuarterlyRevenueStatistics[i].ContractYear;
                        quarterlyRevenueSheet.Cells[i + 2, 2].Value = QuarterlyRevenueStatistics[i].Quarter;
                        quarterlyRevenueSheet.Cells[i + 2, 3].Value = QuarterlyRevenueStatistics[i].Revenue;
                    }

                    var quarterlyRevenueRange = quarterlyRevenueSheet.Cells[1, 1, QuarterlyRevenueStatistics.Count + 2, 3];
                    quarterlyRevenueRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    quarterlyRevenueRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    quarterlyRevenueRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    quarterlyRevenueRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    quarterlyRevenueRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    quarterlyRevenueRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    // Contract Type Statistics
                    var contractTypeSheet = package.Workbook.Worksheets.Add("Contract Type Statistics");
                    contractTypeSheet.Cells[1, 1].Value = "Contract Type";
                    contractTypeSheet.Cells[1, 2].Value = "Revenue";

                    for (int i = 0; i < ContractTypeStatistics.Count; i++)
                    {
                        contractTypeSheet.Cells[i + 2, 1].Value = ContractTypeStatistics[i].ContractType;
                        contractTypeSheet.Cells[i + 2, 2].Value = ContractTypeStatistics[i].Revenue;
                    }

                    var contractTypeRange = contractTypeSheet.Cells[1, 1, ContractTypeStatistics.Count + 2, 2];
                    contractTypeRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    contractTypeRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    contractTypeRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    contractTypeRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    contractTypeRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    contractTypeRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    // Partner Statistics
                    var partnerSheet = package.Workbook.Worksheets.Add("Partner Statistics");
                    partnerSheet.Cells[1, 1].Value = "Partner Name";
                    partnerSheet.Cells[1, 2].Value = "Contract Count";

                    for (int i = 0; i < PartnerStatistics.Count; i++)
                    {
                        partnerSheet.Cells[i + 2, 1].Value = PartnerStatistics[i].PartnerName;
                        partnerSheet.Cells[i + 2, 2].Value = PartnerStatistics[i].ContractCount;
                    }

                    var partnerRange = partnerSheet.Cells[1, 1, PartnerStatistics.Count + 2, 2];
                    partnerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    partnerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    partnerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    partnerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    partnerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    partnerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    // Auto-fit columns
                    statusSheet.Cells[statusSheet.Dimension.Address].AutoFitColumns();
                    quarterSheet.Cells[quarterSheet.Dimension.Address].AutoFitColumns();
                    monthlyRevenueSheet.Cells[monthlyRevenueSheet.Dimension.Address].AutoFitColumns();
                    quarterlyRevenueSheet.Cells[quarterlyRevenueSheet.Dimension.Address].AutoFitColumns();
                    contractTypeSheet.Cells[contractTypeSheet.Dimension.Address].AutoFitColumns();
                    partnerSheet.Cells[partnerSheet.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    var fileName = $"ThongKe_{DateTime.Now:dd-MM-yyyy_HH:mm:ss}.xlsx";

                    // Ghi log cho hành động tải file Excel
                    var email = User.FindFirstValue(ClaimTypes.Email);
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (user != null)
                    {
                        var statsData = JsonSerializer.Serialize(new
                        {
                            StatusCount = StatusStatistics.Count,
                            QuarterCount = QuarterStatistics.Count,
                            MonthlyRevenueCount = MonthlyRevenueStatistics.Count,
                            QuarterlyRevenueCount = QuarterlyRevenueStatistics.Count,
                            ContractTypeCount = ContractTypeStatistics.Count,
                            PartnerCount = PartnerStatistics.Count
                        });
                        await LogAuditAsync(
                            userId: user.Id,
                            table: "",
                            action: "Download",
                            note: $"Tải file Excel thống kê hợp đồng (Status: {StatusStatistics.Count}, Quarters: {QuarterStatistics.Count}, Monthly Revenue: {MonthlyRevenueStatistics.Count}, Quarterly Revenue: {QuarterlyRevenueStatistics.Count}, Contract Types: {ContractTypeStatistics.Count}, Partners: {PartnerStatistics.Count})",
                            data: statsData
                        );
                    }

                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error generating Excel file: " + ex.Message);
            }
        }

        private async Task LogAuditAsync(string userId, string table, string action, string note, string data)
        {
            var auditLog = new AuditLog
            {
                UserID = userId,
                Tables = table,
                Action = action,
                Note = note,
                Data = data,
                CreateDate = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public class StatusStats
        {
            public string ContractStatus { get; set; }
            public int ContractCount { get; set; }
        }

        public class QuarterStats
        {
            public int Quarter { get; set; }
            public string ContractYear { get; set; }
            public int ContractCount { get; set; }
        }

        public class MonthlyRevenueStats
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public double Revenue { get; set; }
        }

        public class QuarterlyRevenueStats
        {
            public int Quarter { get; set; }
            public string ContractYear { get; set; }
            public double Revenue { get; set; }
        }

        public class ContractTypeStats
        {
            public string ContractType { get; set; }
            public double Revenue { get; set; }
        }

        public class PartnerStats
        {
            public string PartnerName { get; set; }
            public int ContractCount { get; set; }
        }
    }
}