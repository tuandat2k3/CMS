using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Models
{
    public class Assignment
    {
        [Key]
        public int AutoID { get; set; }

        [ForeignKey("Contract")]
        public int? ContractID { get; set; }

        [ForeignKey("User")]
        public string UserID { get; set; }

        public DateTime? AssignTime { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        public bool IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string LastUpdateBy { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        // Navigation properties
        public Contract Contract { get; set; }
        public User User { get; set; }
        public ICollection<Contract> Contracts { get; set; }
    }
}