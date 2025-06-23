using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.Data;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace CMS.Pages.Manager
{
    public class PartnerModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartnerModel> _logger;
        public string? ErrorMessage { get; set; }

        public PartnerModel(ApplicationDbContext context, ILogger<PartnerModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Partner> Partners { get; set; } = new();
        public bool IsLoaded { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var query = _context.Partners
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.CompanyName)
                    .AsQueryable();

                Partners = await query
                    .OrderByDescending(c => c.CreateDate)
                    .ToListAsync();
                _logger.LogInformation("Loaded {Count} partners", Partners.Count);
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading partners");
                ErrorMessage = $"Đã xảy ra lỗi khi tải dữ liệu: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetPartnerDetailAsync(int id)
        {
            try
            {
                var partner = await _context.Partners
                    .FirstOrDefaultAsync(c => c.AutoID == id);

                if (partner == null)
                {
                    _logger.LogWarning("Partner not found: Id={Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("Retrieved details for partner: Id={Id}", id);
                return new JsonResult(new
                {
                    autoID = partner.AutoID,
                    userCode = partner.UserCode,
                    company = partner.Company,
                    companyName = partner.CompanyName,
                    companyType = partner.CompanyType,
                    companyDescription = partner.CompanyDescription,
                    companyAddress = partner.CompanyAddress,
                    companyAddressCountry = partner.CompanyAddressCountry,
                    companyAddressCity = partner.CompanyAddressCity,
                    tradingOffice = partner.TradingOffice,
                    representative = partner.Representative,
                    representativePosition = partner.RepresentativePosition,
                    phone = partner.Phone,
                    email = partner.Email,
                    msdn = partner.MSDN,
                    msdnBy = partner.MSDNBy,
                    isActive = partner.IsActive,
                    isDeleted = partner.IsDeleted,
                    createBy = partner.CreateBy,
                    createDate = partner.CreateDate,
                    lastUpdateBy = partner.LastUpdateBy,
                    lastUpdateDate = partner.LastUpdateDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving partner details: Id={Id}", id);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải chi tiết: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostAddPartnerAsync([FromBody] AddPartnerModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for adding partner: {Errors}", string.Join("; ", errors));
                return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
            }

            try
            {
                _logger.LogInformation("Adding new partner: {CompanyName}", model.CompanyName);
                var partner = new Partner
                {
                    UserCode = model.UserCode,
                    Company = model.Company,
                    CompanyName = model.CompanyName,
                    CompanyType = model.CompanyType,
                    CompanyDescription = model.CompanyDescription,
                    CompanyAddress = model.CompanyAddress,
                    CompanyAddressCountry = model.CompanyAddressCountry,
                    CompanyAddressCity = model.CompanyAddressCity,
                    TradingOffice = model.TradingOffice,
                    Representative = model.Representative,
                    RepresentativePosition = model.RepresentativePosition,
                    Phone = model.Phone,
                    Email = model.Email,
                    MSDN = model.MSDN,
                    MSDNBy = model.MSDNBy,
                    IsActive = model.IsActive,
                    IsDeleted = model.IsDeleted,
                    CreateBy = User.Identity?.Name ?? "System",
                    CreateDate = DateTime.Now,
                    LastUpdateBy = User.Identity?.Name ?? "System",
                    LastUpdateDate = DateTime.Now
                };

                _context.Partners.Add(partner);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Partner added successfully: Id={Id}", partner.AutoID);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding partner: {CompanyName}", model.CompanyName);
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm partner: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostUpdatePartnerAsync([FromBody] UpdatePartnerModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for updating partner: {Errors}", string.Join("; ", errors));
                return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
            }

            try
            {
                _logger.LogInformation("Updating partner with Id={Id}", model.Id);
                var partner = await _context.Partners.FindAsync(model.Id);
                if (partner == null)
                {
                    _logger.LogWarning("Partner not found for update: Id={Id}", model.Id);
                    return NotFound(new { success = false, message = "Partner không tồn tại hoặc đã bị xóa" });
                }

                partner.UserCode = model.UserCode;
                partner.Company = model.Company;
                partner.CompanyName = model.CompanyName;
                partner.CompanyType = model.CompanyType;
                partner.CompanyDescription = model.CompanyDescription;
                partner.CompanyAddress = model.CompanyAddress;
                partner.CompanyAddressCountry = model.CompanyAddressCountry;
                partner.CompanyAddressCity = model.CompanyAddressCity;
                partner.TradingOffice = model.TradingOffice;
                partner.Representative = model.Representative;
                partner.RepresentativePosition = model.RepresentativePosition;
                partner.Phone = model.Phone;
                partner.Email = model.Email;
                partner.MSDN = model.MSDN;
                partner.MSDNBy = model.MSDNBy;
                partner.IsActive = model.IsActive;
                partner.IsDeleted = model.IsDeleted;
                partner.LastUpdateBy = User.Identity?.Name ?? "System";
                partner.LastUpdateDate = DateTime.Now;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Partner updated successfully: Id={Id}", model.Id);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating partner: Id={Id}", model.Id);
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật partner: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostDeletePartnerAsync([FromBody] DeletePartnerModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for deleting partner: {Errors}", string.Join("; ", errors));
                return BadRequest(new { success = false, message = $"Dữ liệu không hợp lệ: {string.Join("; ", errors)}" });
            }

            try
            {
                _logger.LogInformation("Deleting partner with Id={Id}", model.Id);
                var partner = await _context.Partners.FindAsync(model.Id);
                if (partner == null)
                {
                    _logger.LogWarning("Partner not found for deletion: Id={Id}", model.Id);
                    return NotFound(new { success = false, message = "Partner không tồn tại hoặc đã bị xóa" });
                }

                partner.IsDeleted = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Partner deleted successfully: Id={Id}", model.Id);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting partner: Id={Id}", model.Id);
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa: {ex.Message}" });
            }
        }

        public class AddPartnerModel
        {
            [Required(ErrorMessage = "Company Name is required")]
            public string CompanyName { get; set; }
            public string? UserCode { get; set; }
            public string? Company { get; set; }
            public string? CompanyType { get; set; }
            public string? CompanyDescription { get; set; }
            public string? CompanyAddress { get; set; }
            public string? CompanyAddressCountry { get; set; }
            public string? CompanyAddressCity { get; set; }
            public string? TradingOffice { get; set; }
            public string? Representative { get; set; }
            public string? RepresentativePosition { get; set; }
            public string? Phone { get; set; }
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string? Email { get; set; }
            public string? MSDN { get; set; }
            public string? MSDNBy { get; set; }
            [Required]
            public bool IsActive { get; set; }
            [Required]
            public bool IsDeleted { get; set; }
        }

        public class UpdatePartnerModel
        {
            [Required]
            public int Id { get; set; }
            [Required(ErrorMessage = "Company Name is required")]
            public string CompanyName { get; set; }
            public string? UserCode { get; set; }
            public string? Company { get; set; }
            public string? CompanyType { get; set; }
            public string? CompanyDescription { get; set; }
            public string? CompanyAddress { get; set; }
            public string? CompanyAddressCountry { get; set; }
            public string? CompanyAddressCity { get; set; }
            public string? TradingOffice { get; set; }
            public string? Representative { get; set; }
            public string? RepresentativePosition { get; set; }
            public string? Phone { get; set; }
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string? Email { get; set; }
            public string? MSDN { get; set; }
            public string? MSDNBy { get; set; }
            [Required]
            public bool IsActive { get; set; }
            [Required]
            public bool IsDeleted { get; set; }
        }

        public class DeletePartnerModel
        {
            [Required]
            public int Id { get; set; }
        }
    }
}