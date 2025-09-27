using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementAPI;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellFromInventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SellFromInventoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/SellFromInventory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSalesRecords()
        {
            var salesRecords = await _context.SellFromInventories
                .Include(s => s.Inventory)
                .ThenInclude(i => i.VehicleModel)
                .Select(s => new
                {
                    s.Id,
                    s.InventoryId,
                    VehicleModelName = s.Inventory.VehicleModel.Name,
                    VehicleModelBrand = s.Inventory.VehicleModel.Brand,
                    VehicleModelColor = s.Inventory.VehicleModel.Color,
                    s.QuantitySold,
                    s.SellPrice,
                    s.TotalAmount,
                    s.CustomerName,
                    s.CustomerPhone,
                    s.SaleDate,
                    s.Notes,
                    UnitCost = s.Inventory.UnitPrice,
                    Profit = s.TotalAmount - (s.QuantitySold * s.Inventory.UnitPrice)
                })
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return Ok(salesRecords);
        }

        // GET: api/SellFromInventory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetSaleRecord(int id)
        {
            var saleRecord = await _context.SellFromInventories
                .Include(s => s.Inventory)
                .ThenInclude(i => i.VehicleModel)
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.InventoryId,
                    VehicleModelName = s.Inventory.VehicleModel.Name,
                    VehicleModelBrand = s.Inventory.VehicleModel.Brand,
                    VehicleModelColor = s.Inventory.VehicleModel.Color,
                    s.QuantitySold,
                    s.SellPrice,
                    s.TotalAmount,
                    s.CustomerName,
                    s.CustomerPhone,
                    s.SaleDate,
                    s.Notes,
                    UnitCost = s.Inventory.UnitPrice,
                    Profit = s.TotalAmount - (s.QuantitySold * s.Inventory.UnitPrice)
                })
                .FirstOrDefaultAsync();

            if (saleRecord == null)
            {
                return NotFound();
            }

            return Ok(saleRecord);
        }

        // POST: api/SellFromInventory (Bán hàng từ tồn kho)
        [HttpPost]
        public async Task<ActionResult<SellFromInventory>> PostSaleFromInventory(CreateSaleRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if inventory exists and has enough quantity
                var inventory = await _context.Inventories
                    .Include(i => i.VehicleModel)
                    .FirstOrDefaultAsync(i => i.Id == request.InventoryId);

                if (inventory == null)
                {
                    return BadRequest("Inventory record not found");
                }

                if (inventory.Quantity < request.QuantitySold)
                {
                    return BadRequest($"Insufficient inventory. Available: {inventory.Quantity}, Requested: {request.QuantitySold}");
                }

                // Calculate total amount
                var totalAmount = request.QuantitySold * request.SellPrice;

                // Create sale record
                var saleRecord = new SellFromInventory
                {
                    InventoryId = request.InventoryId,
                    QuantitySold = request.QuantitySold,
                    SellPrice = request.SellPrice,
                    TotalAmount = totalAmount,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    SaleDate = DateTime.Now,
                    Notes = request.Notes
                };

                _context.SellFromInventories.Add(saleRecord);

                // Update inventory quantity
                inventory.Quantity -= request.QuantitySold;
                inventory.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return sale record with additional info
                var result = new
                {
                    saleRecord.Id,
                    saleRecord.InventoryId,
                    VehicleModelName = inventory.VehicleModel.Name,
                    VehicleModelBrand = inventory.VehicleModel.Brand,
                    VehicleModelColor = inventory.VehicleModel.Color,
                    saleRecord.QuantitySold,
                    saleRecord.SellPrice,
                    saleRecord.TotalAmount,
                    saleRecord.CustomerName,
                    saleRecord.CustomerPhone,
                    saleRecord.SaleDate,
                    saleRecord.Notes,
                    UnitCost = inventory.UnitPrice,
                    Profit = saleRecord.TotalAmount - (saleRecord.QuantitySold * inventory.UnitPrice),
                    RemainingInventory = inventory.Quantity
                };

                return CreatedAtAction("GetSaleRecord", new { id = saleRecord.Id }, result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error processing sale: {ex.Message}");
            }
        }

        // GET: api/SellFromInventory/sales-summary
        [HttpGet("sales-summary")]
        public async Task<ActionResult<object>> GetSalesSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var startDate = fromDate ?? DateTime.Today.AddDays(-30);
            var endDate = toDate ?? DateTime.Today.AddDays(1);

            var salesSummary = await _context.SellFromInventories
                .Include(s => s.Inventory)
                .Where(s => s.SaleDate >= startDate && s.SaleDate < endDate)
                .GroupBy(s => 1) // Group all records
                .Select(g => new
                {
                    TotalSales = g.Sum(s => s.TotalAmount),
                    TotalQuantitySold = g.Sum(s => s.QuantitySold),
                    TotalProfit = g.Sum(s => s.TotalAmount - (s.QuantitySold * s.Inventory.UnitPrice)),
                    TotalTransactions = g.Count(),
                    AverageTransactionValue = g.Average(s => s.TotalAmount),
                    FromDate = startDate.ToString("yyyy-MM-dd"),
                    ToDate = endDate.AddDays(-1).ToString("yyyy-MM-dd")
                })
                .FirstOrDefaultAsync();

            if (salesSummary == null)
            {
                return Ok(new
                {
                    TotalSales = 0m,
                    TotalQuantitySold = 0,
                    TotalProfit = 0m,
                    TotalTransactions = 0,
                    AverageTransactionValue = 0m,
                    FromDate = startDate.ToString("yyyy-MM-dd"),
                    ToDate = endDate.AddDays(-1).ToString("yyyy-MM-dd")
                });
            }

            return Ok(salesSummary);
        }

        // GET: api/SellFromInventory/top-selling
        [HttpGet("top-selling")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopSellingVehicles([FromQuery] int limit = 10)
        {
            var topSelling = await _context.SellFromInventories
                .Include(s => s.Inventory)
                .ThenInclude(i => i.VehicleModel)
                .GroupBy(s => new { s.Inventory.VehicleModelId, s.Inventory.VehicleModel.Name, s.Inventory.VehicleModel.Brand, s.Inventory.VehicleModel.Color })
                .Select(g => new
                {
                    VehicleModelId = g.Key.VehicleModelId,
                    VehicleModelName = g.Key.Name,
                    VehicleModelBrand = g.Key.Brand,
                    VehicleModelColor = g.Key.Color,
                    TotalQuantitySold = g.Sum(s => s.QuantitySold),
                    TotalRevenue = g.Sum(s => s.TotalAmount),
                    TotalProfit = g.Sum(s => s.TotalAmount - (s.QuantitySold * s.Inventory.UnitPrice)),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(limit)
                .ToListAsync();

            return Ok(topSelling);
        }

        // DELETE: api/SellFromInventory/5 (Cancel sale - restore inventory)
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelSale(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var saleRecord = await _context.SellFromInventories
                    .Include(s => s.Inventory)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (saleRecord == null)
                {
                    return NotFound();
                }

                // Restore inventory quantity
                saleRecord.Inventory.Quantity += saleRecord.QuantitySold;
                saleRecord.Inventory.LastUpdated = DateTime.Now;

                // Remove sale record
                _context.SellFromInventories.Remove(saleRecord);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Sale cancelled and inventory restored", RestoredQuantity = saleRecord.QuantitySold });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error cancelling sale: {ex.Message}");
            }
        }
    }

    public class CreateSaleRequest
    {
        public int InventoryId { get; set; }
        public int QuantitySold { get; set; }
        public decimal SellPrice { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Notes { get; set; }
    }
}