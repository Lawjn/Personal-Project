using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementAPI;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Inventory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetInventories()
        {
            var inventories = await _context.Inventories
                .Include(i => i.VehicleModel)
                .Select(i => new
                {
                    i.Id,
                    i.VehicleModelId,
                    VehicleModelName = i.VehicleModel.Name,
                    VehicleModelBrand = i.VehicleModel.Brand,
                    VehicleModelColor = i.VehicleModel.Color,
                    i.Quantity,
                    i.UnitPrice,
                    i.ImportDate,
                    i.LastUpdated,
                    i.Notes
                })
                .ToListAsync();

            return Ok(inventories);
        }

        // GET: api/Inventory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetInventory(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.VehicleModel)
                .Where(i => i.Id == id)
                .Select(i => new
                {
                    i.Id,
                    i.VehicleModelId,
                    VehicleModelName = i.VehicleModel.Name,
                    VehicleModelBrand = i.VehicleModel.Brand,
                    VehicleModelColor = i.VehicleModel.Color,
                    i.Quantity,
                    i.UnitPrice,
                    i.ImportDate,
                    i.LastUpdated,
                    i.Notes
                })
                .FirstOrDefaultAsync();

            if (inventory == null)
            {
                return NotFound();
            }

            return Ok(inventory);
        }

        // POST: api/Inventory (Nhập kho)
        [HttpPost]
        public async Task<ActionResult<Inventory>> PostInventory(CreateInventoryRequest request)
        {
            // Check if VehicleModel exists
            var vehicleModel = await _context.VehicleModels.FindAsync(request.VehicleModelId);
            if (vehicleModel == null)
            {
                return BadRequest("Vehicle Model not found");
            }

            // Check if inventory already exists for this vehicle model
            var existingInventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.VehicleModelId == request.VehicleModelId);

            if (existingInventory != null)
            {
                // Update existing inventory
                existingInventory.Quantity += request.Quantity;
                existingInventory.UnitPrice = request.UnitPrice; // Update with latest price
                existingInventory.LastUpdated = DateTime.Now;
                existingInventory.Notes = request.Notes;

                _context.Entry(existingInventory).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(existingInventory);
            }
            else
            {
                // Create new inventory record
                var inventory = new Inventory
                {
                    VehicleModelId = request.VehicleModelId,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice,
                    ImportDate = DateTime.Now,
                    Notes = request.Notes
                };

                _context.Inventories.Add(inventory);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetInventory", new { id = inventory.Id }, inventory);
            }
        }

        // PUT: api/Inventory/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInventory(int id, UpdateInventoryRequest request)
        {
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            inventory.Quantity = request.Quantity;
            inventory.UnitPrice = request.UnitPrice;
            inventory.LastUpdated = DateTime.Now;
            inventory.Notes = request.Notes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Inventory/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            // Check if there are any sales records
            var hasSales = await _context.SellFromInventories.AnyAsync(s => s.InventoryId == id);
            if (hasSales)
            {
                return BadRequest("Cannot delete inventory with existing sales records");
            }

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Inventory/low-stock/{threshold}
        [HttpGet("low-stock/{threshold}")]
        public async Task<ActionResult<IEnumerable<object>>> GetLowStockItems(int threshold = 10)
        {
            var lowStockItems = await _context.Inventories
                .Include(i => i.VehicleModel)
                .Where(i => i.Quantity <= threshold)
                .Select(i => new
                {
                    i.Id,
                    i.VehicleModelId,
                    VehicleModelName = i.VehicleModel.Name,
                    VehicleModelBrand = i.VehicleModel.Brand,
                    VehicleModelColor = i.VehicleModel.Color,
                    i.Quantity,
                    i.UnitPrice,
                    Status = i.Quantity == 0 ? "Out of Stock" : "Low Stock"
                })
                .ToListAsync();

            return Ok(lowStockItems);
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventories.Any(e => e.Id == id);
        }
    }

    public class CreateInventoryRequest
    {
        public int VehicleModelId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateInventoryRequest
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }
}