using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace CMS.Models
{
    public class Permission
    {
        [Key]
        public int AutoID { get; set; }

        [ForeignKey("Role")]
        public Guid? RoleID { get; set; }

        [ForeignKey("Category")]
        public int? CateID { get; set; }

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
        public Category Category { get; set; }
    }
}