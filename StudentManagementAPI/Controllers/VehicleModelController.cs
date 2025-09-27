using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementAPI;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleModelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleModelController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/VehicleModel
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleModel>>> GetVehicleModels()
        {
            return await _context.VehicleModels.ToListAsync();
        }

        // GET: api/VehicleModel/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleModel>> GetVehicleModel(int id)
        {
            var vehicleModel = await _context.VehicleModels.FindAsync(id);

            if (vehicleModel == null)
            {
                return NotFound();
            }

            return vehicleModel;
        }

        // GET: api/VehicleModel/with-inventory
        [HttpGet("with-inventory")]
        public async Task<ActionResult<IEnumerable<object>>> GetVehicleModelsWithInventory()
        {
            var vehicleModelsWithInventory = await _context.VehicleModels
                .Include(v => v.Inventories)
                .Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.Brand,
                    v.Color,
                    v.Price,
                    v.CreatedAt,
                    TotalQuantity = v.Inventories.Sum(i => i.Quantity),
                    InventoryRecords = v.Inventories.Select(i => new
                    {
                        i.Id,
                        i.Quantity,
                        i.UnitPrice,
                        i.ImportDate,
                        i.LastUpdated
                    })
                })
                .ToListAsync();

            return Ok(vehicleModelsWithInventory);
        }

        // POST: api/VehicleModel
        [HttpPost]
        public async Task<ActionResult<VehicleModel>> PostVehicleModel(CreateVehicleModelRequest request)
        {
            // Check if vehicle model with same name already exists
            var existingModel = await _context.VehicleModels
                .FirstOrDefaultAsync(v => v.Name.ToLower() == request.Name.ToLower());

            if (existingModel != null)
            {
                return BadRequest("Vehicle model with this name already exists");
            }

            var vehicleModel = new VehicleModel
            {
                Name = request.Name,
                Brand = request.Brand,
                Color = request.Color,
                Price = request.Price,
                CreatedAt = DateTime.Now
            };

            _context.VehicleModels.Add(vehicleModel);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVehicleModel", new { id = vehicleModel.Id }, vehicleModel);
        }

        // PUT: api/VehicleModel/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicleModel(int id, UpdateVehicleModelRequest request)
        {
            var vehicleModel = await _context.VehicleModels.FindAsync(id);
            if (vehicleModel == null)
            {
                return NotFound();
            }

            // Check if another vehicle model with same name exists
            var existingModel = await _context.VehicleModels
                .FirstOrDefaultAsync(v => v.Name.ToLower() == request.Name.ToLower() && v.Id != id);

            if (existingModel != null)
            {
                return BadRequest("Vehicle model with this name already exists");
            }

            vehicleModel.Name = request.Name;
            vehicleModel.Brand = request.Brand;
            vehicleModel.Color = request.Color;
            vehicleModel.Price = request.Price;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleModelExists(id))
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

        // DELETE: api/VehicleModel/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicleModel(int id)
        {
            var vehicleModel = await _context.VehicleModels.FindAsync(id);
            if (vehicleModel == null)
            {
                return NotFound();
            }

            // Check if there are any inventory records
            var hasInventory = await _context.Inventories.AnyAsync(i => i.VehicleModelId == id);
            if (hasInventory)
            {
                return BadRequest("Cannot delete vehicle model with existing inventory records");
            }

            _context.VehicleModels.Remove(vehicleModel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VehicleModelExists(int id)
        {
            return _context.VehicleModels.Any(e => e.Id == id);
        }
    }

    public class CreateVehicleModelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class UpdateVehicleModelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}