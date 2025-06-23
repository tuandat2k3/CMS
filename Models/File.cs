using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Models
{
    public class File
    {
        [Key]
        public int AutoID { get; set; }

        [ForeignKey("Folder")]
        public int? FolderID { get; set; }

        [ForeignKey("Contract")]
        public int? ContractID { get; set; }

        [ForeignKey("Invoice")]
        public int? InvoiceID { get; set; }
        public string StaffID { get; set; }

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

        public string FilePath { get; set; }
        public string FileType { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        public bool IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string LastUpdateBy { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        // Navigation properties
        public Folder Folder { get; set; }
        public Contract Contract { get; set; }
        public Invoice Invoice { get; set; }
        public Corporation Corporation { get; set; }
        public Company Company { get; set; }
        public Branch Branch { get; set; }
        public Department Department { get; set; }
        public Position Position { get; set; }
        public ICollection<Contract> Contracts { get; set; }
    }
}