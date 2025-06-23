namespace CMS.ViewModels
{
    public class AuditLogViewModel
    {
        public string? UserID { get; set; }
        public string? Action { get; set; }
        public string? Note { get; set; }
        public string? Data { get; set; }
        public DateTime? CreateDate { get; set; }
    }
}
