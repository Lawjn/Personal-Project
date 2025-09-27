using System.ComponentModel.DataAnnotations;

namespace StudentManagementAPI.Models
{
    public class VehicleModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Brand { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Color { get; set; } = string.Empty;
        
        [Required]
        public decimal Price { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation property
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}