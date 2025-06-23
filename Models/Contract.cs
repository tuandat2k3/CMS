using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Models
{
    public class Contract
    {
        [Key]
        public int AutoID { get; set; }
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

        [ForeignKey("Partner")]
        public int? PartnerID { get; set; }

        [ForeignKey("File")]
        public int? FilesAutoID { get; set; }

        [ForeignKey("Invoice")]
        public int? InvoicesAutoID { get; set; }

        [ForeignKey("Assignment")]
        public int? AssignmentsAutoID { get; set; }

        public string ContractName { get; set; }
        public string ContractType { get; set; }
        public string ContractNumber { get; set; }
        public string ContractYear { get; set; }
        public string ContractDate { get; set; }
        public string? ContractTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? ContractValue { get; set; }
        public string ContractStatus { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        public bool IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? LastUpdateBy { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        // Navigation properties
        public Corporation Corporation { get; set; }
        public Company Company { get; set; }
        public Branch Branch { get; set; }
        public Department Department { get; set; }
        public Position Position { get; set; }
        public Partner Partner { get; set; }
        public File File { get; set; }
        public Invoice Invoice { get; set; }
        public Assignment Assignment { get; set; }
        public ICollection<Assignment> Assignments { get; set; }
        public ICollection<File> Files { get; set; } = new List<File>();
        public ICollection<Invoice> Invoices { get; set; }
        public List<ContractRejection> ContractRejections { get; set; }
    }
}