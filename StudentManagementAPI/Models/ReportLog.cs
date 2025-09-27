using System.ComponentModel.DataAnnotations;

namespace StudentManagementAPI.Models
{
    public class ReportLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ReportType { get; set; } = string.Empty;
        
        [Required]
        public DateTime FromDate { get; set; }
        
        [Required]
        public DateTime ToDate { get; set; }
        
        [Required]
        public decimal TotalRevenue { get; set; }
        
        [Required]
        public decimal TotalProfit { get; set; }
        
        [Required]
        public int TotalQuantitySold { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string? CreatedBy { get; set; }
        
        [StringLength(500)]
        public string? AdditionalData { get; set; } // JSON format for flexible data storage
    }
}