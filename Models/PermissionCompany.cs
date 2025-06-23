using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace CMS.Models
{
    public class PermissionCompany
    {
        [Key]
        public int AutoID { get; set; }

        [ForeignKey("Role")]
        public Guid? RoleID { get; set; }

        [ForeignKey("Corporation")]
        public int? CorporationID { get; set; }

        [ForeignKey("Company")]
        public int? CompanyID { get; set; }

        [ForeignKey("Branch")]
        public int? BranchID { get; set; }

        [ForeignKey("Department")]
        public int? DepartmentID { get; set; }

        [ForeignKey("Position")]
        public int? PositionID { get; set; }

        public bool? IsView { get; set; }
        public bool? IsStaffAction { get; set; }
        public bool? IsManagerAction { get; set; }
        public bool? IsAdminAction { get; set; }
        public bool? IsEdit { get; set; }
        public bool? IsCreate { get; set; }
        public bool? IsDelete { get; set; }
        public bool? IsFilesActions { get; set; }
        public string LastUpdateBy { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        // Navigation properties
        public Role Role { get; set; }
        public Corporation Corporation { get; set; }
        public Company Company { get; set; }
        public Branch Branch { get; set; }
        public Department Department { get; set; }
        public Position Position { get; set; }
    }
}