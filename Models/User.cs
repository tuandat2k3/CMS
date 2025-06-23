using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; }
        public string FullName { get; set; }
        public string StaffCode { get; set; }
        public string Address { get; set; }
        public string Station { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        [MaxLength(256)]
        public string? NormalizedUserName { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(256)]
        public string? NormalizedEmail { get; set; }

        public bool EmailConfirmed { get; set; }
        public string? Password { get; set; }
        public string? SecurityStamp { get; set; }
        public string? ConcurrencyStamp { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }

        [ForeignKey("Branch")]
        public int? BranchID { get; set; }

        [ForeignKey("Company")]
        public int? CompanyID { get; set; }

        [ForeignKey("Corporation")]
        public int? CorporationID { get; set; }

        [ForeignKey("Department")]
        public int? DepartmentID { get; set; }

        [ForeignKey("Position")]
        public int? PositionID { get; set; }

        // Navigation properties
        public Branch Branch { get; set; }
        public Company Company { get; set; }
        public Corporation Corporation { get; set; }
        public Department Department { get; set; }
        public Position Position { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<Assignment> Assignments { get; set; }
    }
}