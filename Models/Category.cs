using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security;

namespace CMS.Models
{
    public class Category
    {
        [Key]
        public int CateID { get; set; }

        [Required]
        [MaxLength(255)]
        public string CateName { get; set; }
        public string CateCode { get; set; }
        public int? CateOrder { get; set; }

        [ForeignKey("Parent")]
        public int? ParentID { get; set; }
        public int? Type { get; set; }
        public bool? IsComponent { get; set; }
        public bool? IsRoot { get; set; }
        public bool? IsShow { get; set; }
        public bool? IsPrivate { get; set; }
        public bool? IsActive { get; set; }
        public string Url { get; set; }
        public string Descriptions { get; set; }
        public string Descriptions2 { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string LastUpdateBy { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        // Navigation properties
        public Category Parent { get; set; }
        public ICollection<Category> Children { get; set; }
        public ICollection<Permission> Permissions { get; set; }
    }
}