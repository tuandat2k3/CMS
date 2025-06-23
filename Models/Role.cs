using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Models
{
    public class Role
    {
        [Key]
        public Guid RolesID { get; set; }

        [Required]
        [MaxLength(255)]
        public string RoleName { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreatDate { get; set; }

        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<Permission> Permissions { get; set; }
        public ICollection<PermissionCompany> PermissionCompanies { get; set; }
    }
}