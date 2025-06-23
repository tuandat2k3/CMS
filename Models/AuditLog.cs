using System;
using System.ComponentModel.DataAnnotations;

namespace CMS.Models
{
    public class AuditLog
    {
        [Key]
        public int AutoID { get; set; }
        public string UserID { get; set; }
        public string Tables { get; set; }
        public string Action { get; set; }
        public string Note { get; set; }
        public string Data { get; set; }
        public DateTime? CreateDate { get; set; }
    }
}