using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Models
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }

        [ForeignKey("Role")]
        public Guid? RolesID { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Role Role { get; set; }
    }
}