using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace CMS.Models
{
    public class Corporation
    {
        [Key]
        public int AutoID { get; set; }
        public string CorporationName { get; set; }
        public string CorporationSymbol { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string HomePhone { get; set; }
        public string Representative { get; set; }
        public bool? IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        // Navigation properties
        public ICollection<Company> Companies { get; set; }
        public ICollection<Contract> Contracts { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<File> Files { get; set; }
    }
}