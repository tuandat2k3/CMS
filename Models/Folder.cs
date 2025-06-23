using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Models
{
    public class Folder
    {
        [Key]
        public int AutoID { get; set; }
        public string FolderName { get; set; }
        public string FolderType { get; set; }
        public string FolderDescription { get; set; }

        [Required]
        public bool IsRoot { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        public bool IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string LastUpdateBy { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        // Navigation properties
        public ICollection<File> Files { get; set; }
    }
}