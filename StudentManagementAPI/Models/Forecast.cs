using System.ComponentModel.DataAnnotations;

namespace StudentManagementAPI.Models
{
    public class Forecast
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int VehicleModelId { get; set; }
        
        [Required]
        public DateTime ForecastDate { get; set; }
        
        [Required]
        public int PredictedDemand { get; set; }
        
        [Required]
        public decimal PredictedRevenue { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ForecastMethod { get; set; } = string.Empty; // e.g., "Linear", "Seasonal", "Manual"
        
        public decimal? ConfidenceLevel { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [StringLength(200)]
        public string? Notes { get; set; }
        
        // Navigation property
        public virtual VehicleModel VehicleModel { get; set; } = null!;
    }
}