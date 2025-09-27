using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementAPI.Models
{
    public class Inventory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int VehicleModelId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        public DateTime ImportDate { get; set; } = DateTime.Now;
        
        public DateTime? LastUpdated { get; set; }
        
        [StringLength(200)]
        public string? Notes { get; set; }
        
        // Navigation property
        [ForeignKey("VehicleModelId")]
        public virtual VehicleModel VehicleModel { get; set; } = null!;
        
        // Navigation property
        public virtual ICollection<SellFromInventory> SalesRecords { get; set; } = new List<SellFromInventory>();
    }
}