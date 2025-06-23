using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace CMS.Models
{
    public class Department
    {
        [Key]
        public int AutoID { get; set; }

        [ForeignKey("Branch")]
        public int? BranchID { get; set; }
        public string DepartmentName { get; set; }
        public string DepartmentSymbol { get; set; }
        public string DepartmentDescription { get; set; }
        public string Representative { get; set; }
        public bool? IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        // Navigation properties
        public Branch Branch { get; set; }
        public ICollection<Position> Positions { get; set; }
        public ICollection<Contract> Contracts { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<File> Files { get; set; }
        public ICollection<PermissionCompany> PermissionCompanies { get; set; }
    }
}