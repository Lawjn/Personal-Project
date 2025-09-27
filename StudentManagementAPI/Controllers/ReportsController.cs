using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementAPI;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Reports/generate-sales-report
        [HttpPost("generate-sales-report")]
        public async Task<ActionResult<object>> GenerateSalesReport(GenerateReportRequest request)
        {
            var salesData = await _context.SellFromInventories
                .Include(s => s.Inventory)
                .ThenInclude(i => i.VehicleModel)
                .Where(s => s.SaleDate >= request.FromDate && s.SaleDate <= request.ToDate)
                .ToListAsync();

            var totalRevenue = salesData.Sum(s => s.TotalAmount);
            var totalProfit = salesData.Sum(s => s.TotalAmount - (s.QuantitySold * s.Inventory.UnitPrice));
            var totalQuantitySold = salesData.Sum(s => s.QuantitySold);

            // Save report log
            var reportLog = new ReportLog
            {
                ReportType = "Sales Report",
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                TotalRevenue = totalRevenue,
                TotalProfit = totalProfit,
                TotalQuantitySold = totalQuantitySold,
                CreatedAt = DateTime.Now,
                CreatedBy = request.CreatedBy
            };

            _context.ReportLogs.Add(reportLog);
            await _context.SaveChangesAsync();

            // Detailed breakdown by vehicle model
            var vehicleBreakdown = salesData
                .GroupBy(s => new { s.Inventory.VehicleModelId, s.Inventory.VehicleModel.Name, s.Inventory.VehicleModel.Brand, s.Inventory.VehicleModel.Color })
                .Select(g => new
                {
                    VehicleModelId = g.Key.VehicleModelId,
                    VehicleModelName = g.Key.Name,
                    VehicleModelBrand = g.Key.Brand,
                    VehicleModelColor = g.Key.Color,
                    QuantitySold = g.Sum(s => s.QuantitySold),
                    Revenue = g.Sum(s => s.TotalAmount),
                    Profit = g.Sum(s => s.TotalAmount - (s.QuantitySold * s.Inventory.UnitPrice)),
                    TransactionCount = g.Count(),
                    AverageSellingPrice = g.Average(s => s.SellPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var report = new
            {
                ReportId = reportLog.Id,
                Period = new { From = request.FromDate, To = request.ToDate },
                Summary = new
                {
                    TotalRevenue = totalRevenue,
                    TotalProfit = totalProfit,
                    TotalQuantitySold = totalQuantitySold,
                    TotalTransactions = salesData.Count,
                    AverageTransactionValue = salesData.Count > 0 ? totalRevenue / salesData.Count : 0,
                    ProfitMargin = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0
                },
                VehicleBreakdown = vehicleBreakdown,
                GeneratedAt = DateTime.Now,
                GeneratedBy = request.CreatedBy
            };

            return Ok(report);
        }

        // POST: api/Reports/generate-forecast
        [HttpPost("generate-forecast")]
        public async Task<ActionResult<object>> GenerateForecast(GenerateForecastRequest request)
        {
            // Get historical sales data for the specified vehicle model
            var historicalData = await _context.SellFromInventories
                .Include(s => s.Inventory)
                .Where(s => s.Inventory.VehicleModelId == request.VehicleModelId)
                .Where(s => s.SaleDate >= DateTime.Now.AddDays(-request.HistoricalDays))
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    QuantitySold = g.Sum(s => s.QuantitySold),
                    Revenue = g.Sum(s => s.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            if (!historicalData.Any())
            {
                return BadRequest("No historical data available for forecasting");
            }

            // Simple linear forecast calculation
            var avgDailyQuantity = historicalData.Average(h => h.QuantitySold);
            var avgDailyRevenue = historicalData.Average(h => h.Revenue);

            // Calculate trend (simple linear regression)
            var days = historicalData.Count;
            var trend = 0.0;
            if (days > 1)
            {
                var firstHalf = historicalData.Take(days / 2).Average(h => h.QuantitySold);
                var secondHalf = historicalData.Skip(days / 2).Average(h => h.QuantitySold);
                trend = (secondHalf - firstHalf) / (days / 2.0);
            }

            var predictedDemand = (int)Math.Max(0, avgDailyQuantity + (trend * request.ForecastDays));
            var predictedRevenue = (decimal)avgDailyRevenue * request.ForecastDays * ((decimal)predictedDemand / (decimal)Math.Max(1, avgDailyQuantity));

            // Save forecast
            var forecast = new Forecast
            {
                VehicleModelId = request.VehicleModelId,
                ForecastDate = request.ForecastDate,
                PredictedDemand = predictedDemand,
                PredictedRevenue = (decimal)predictedRevenue,
                ForecastMethod = "Linear Trend",
                ConfidenceLevel = CalculateConfidenceLevel(historicalData.Count, trend),
                CreatedAt = DateTime.Now,
                Notes = $"Based on {days} days of historical data"
            };

            _context.Forecasts.Add(forecast);
            await _context.SaveChangesAsync();

            var vehicleModel = await _context.VehicleModels.FindAsync(request.VehicleModelId);

            var result = new
            {
                ForecastId = forecast.Id,
                VehicleModel = new
                {
                    vehicleModel?.Id,
                    vehicleModel?.Name,
                    vehicleModel?.Brand,
                    vehicleModel?.Color
                },
                ForecastPeriod = new
                {
                    ForecastDate = request.ForecastDate,
                    ForecastDays = request.ForecastDays
                },
                Prediction = new
                {
                    PredictedDemand = predictedDemand,
                    PredictedRevenue = predictedRevenue,
                    ConfidenceLevel = forecast.ConfidenceLevel
                },
                HistoricalData = new
                {
                    DataPoints = historicalData.Count,
                    AvgDailyQuantity = avgDailyQuantity,
                    AvgDailyRevenue = avgDailyRevenue,
                    Trend = trend > 0 ? "Increasing" : trend < 0 ? "Decreasing" : "Stable"
                },
                CreatedAt = forecast.CreatedAt
            };

            return Ok(result);
        }

        // GET: api/Reports/report-logs
        [HttpGet("report-logs")]
        public async Task<ActionResult<IEnumerable<ReportLog>>> GetReportLogs([FromQuery] int limit = 50)
        {
            var reportLogs = await _context.ReportLogs
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(reportLogs);
        }

        // GET: api/Reports/forecasts
        [HttpGet("forecasts")]
        public async Task<ActionResult<IEnumerable<object>>> GetForecasts([FromQuery] int? vehicleModelId, [FromQuery] int limit = 20)
        {
            var query = _context.Forecasts.Include(f => f.VehicleModel).AsQueryable();

            if (vehicleModelId.HasValue)
            {
                query = query.Where(f => f.VehicleModelId == vehicleModelId.Value);
            }

            var forecasts = await query
                .OrderByDescending(f => f.CreatedAt)
                .Take(limit)
                .Select(f => new
                {
                    f.Id,
                    f.VehicleModelId,
                    VehicleModelName = f.VehicleModel.Name,
                    VehicleModelBrand = f.VehicleModel.Brand,
                    VehicleModelColor = f.VehicleModel.Color,
                    f.ForecastDate,
                    f.PredictedDemand,
                    f.PredictedRevenue,
                    f.ForecastMethod,
                    f.ConfidenceLevel,
                    f.CreatedAt,
                    f.Notes
                })
                .ToListAsync();

            return Ok(forecasts);
        }

        // GET: api/Reports/inventory-status
        [HttpGet("inventory-status")]
        public async Task<ActionResult<object>> GetInventoryStatus()
        {
            var inventoryStatus = await _context.Inventories
                .Include(i => i.VehicleModel)
                .Select(i => new
                {
                    i.Id,
                    i.VehicleModelId,
                    VehicleModelName = i.VehicleModel.Name,
                    VehicleModelBrand = i.VehicleModel.Brand,
                    VehicleModelColor = i.VehicleModel.Color,
                    CurrentQuantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalValue = i.Quantity * i.UnitPrice,
                    Status = i.Quantity == 0 ? "Out of Stock" : i.Quantity <= 5 ? "Low Stock" : "In Stock",
                    LastUpdated = i.LastUpdated
                })
                .OrderBy(i => i.CurrentQuantity)
                .ToListAsync();

            var summary = new
            {
                TotalItems = inventoryStatus.Count,
                TotalValue = inventoryStatus.Sum(i => i.TotalValue),
                OutOfStock = inventoryStatus.Count(i => i.Status == "Out of Stock"),
                LowStock = inventoryStatus.Count(i => i.Status == "Low Stock"),
                InStock = inventoryStatus.Count(i => i.Status == "In Stock")
            };

            return Ok(new
            {
                Summary = summary,
                InventoryDetails = inventoryStatus
            });
        }

        private decimal CalculateConfidenceLevel(int dataPoints, double trend)
        {
            // Simple confidence calculation based on data points and trend stability
            var baseConfidence = Math.Min(90, 50 + (dataPoints * 2)); // More data points = higher confidence
            var trendPenalty = Math.Abs(trend) > 1 ? 10 : 0; // Volatile trend reduces confidence
            return (decimal)Math.Max(30, baseConfidence - trendPenalty);
        }
    }

    public class GenerateReportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class GenerateForecastRequest
    {
        public int VehicleModelId { get; set; }
        public DateTime ForecastDate { get; set; }
        public int ForecastDays { get; set; } = 30;
        public int HistoricalDays { get; set; } = 90;
    }
}