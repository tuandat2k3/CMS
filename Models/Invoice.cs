using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Models
{
    public class Invoice
    {
        [Key]
        public int AutoID { get; set; }

        [ForeignKey("Contract")]
        public int? ContractID { get; set; }
        public string InvoicesName { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? DueDate { get; set; }
        public double? InvoiceValue { get; set; }
        public string Status { get; set; }

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
        public ICollection<File> Files { get; set; }
        public ICollection<Contract> Contracts { get; set; }
    }
}