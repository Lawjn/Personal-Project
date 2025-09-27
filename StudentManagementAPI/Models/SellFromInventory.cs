using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementAPI.Models
{
    public class SellFromInventory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int InventoryId { get; set; }
        
        [Required]
        public int QuantitySold { get; set; }
        
        [Required]
        public decimal SellPrice { get; set; }
        
        [Required]
        public decimal TotalAmount { get; set; }
        
        [StringLength(100)]
        public string? CustomerName { get; set; }
        
        [StringLength(20)]
        public string? CustomerPhone { get; set; }
        
        public DateTime SaleDate { get; set; } = DateTime.Now;
        
        [StringLength(200)]
        public string? Notes { get; set; }
        
        // Calculated properties
        public decimal Profit => TotalAmount - (QuantitySold * (Inventory?.UnitPrice ?? 0));
        
        // Navigation property
        [ForeignKey("InventoryId")]
        public virtual Inventory Inventory { get; set; } = null!;
    }
}