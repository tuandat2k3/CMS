namespace CMS.Models
{
    public class ContractRejection
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string? Reason { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime RejectedDate { get; set; }

        public Contract Contract { get; set; } 
    }
}