using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace CMS.Models
{
    public class Branch
    {
        [Key]
        public int AutoID { get; set; }

        [ForeignKey("Company")]
        public int? CompanyID { get; set; }
        public string BranchName { get; set; }
        public string BranchSymbol { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string HomePhone { get; set; }
        public string Representative { get; set; }
        public bool? IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        // Navigation properties
        public Company Company { get; set; }
        public ICollection<Department> Departments { get; set; }
        public ICollection<Contract> Contracts { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<File> Files { get; set; }
        public ICollection<PermissionCompany> PermissionCompanies { get; set; }
    }
}