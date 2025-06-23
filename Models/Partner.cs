using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Models
{
    public class Partner
    {
        [Key]
        public int AutoID { get; set; }
        public string UserCode { get; set; }
        public string Company { get; set; }
        public string CompanyName { get; set; }
        public string CompanyType { get; set; }
        public string CompanyDescription { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyAddressCountry { get; set; }
        public string CompanyAddressCity { get; set; }
        public string TradingOffice { get; set; }
        public string Representative { get; set; }
        public string RepresentativePosition { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string MSDN { get; set; }
        public string MSDNBy { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        public bool IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string LastUpdateBy { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        // Navigation properties
        public ICollection<Contract> Contracts { get; set; }
    }
}