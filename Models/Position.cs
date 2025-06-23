using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace CMS.Models
{
    public class Position
    {
        [Key]
        public int AutoID { get; set; }

        [ForeignKey("Department")]
        public int? DepartmentID { get; set; }
        public string PositionName { get; set; }
        public string PositionSymbol { get; set; }
        public string Descriptions { get; set; }
        public bool? IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        // Navigation properties
        public Department Department { get; set; }
        public ICollection<Contract> Contracts { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<File> Files { get; set; }
        public ICollection<PermissionCompany> PermissionCompanies { get; set; }
    }
}