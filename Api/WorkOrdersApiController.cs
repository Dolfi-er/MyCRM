using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;
using MyCRM.Services;

namespace MyCRM.Api
{
    [Route("api/orders")]
    [ApiController]
    public class WorkOrdersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductionCalculationService _calculationService;

        public WorkOrdersApiController(
            ApplicationDbContext context,
            IProductionCalculationService calculationService)
        {
            _context = context;
            _calculationService = calculationService;
        }

        // GET: api/orders?status=active&date=today
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkOrder>>> GetWorkOrders(
            [FromQuery] string status = null, 
            [FromQuery] string date = null)
        {
            IQueryable<WorkOrder> query = _context.WorkOrders
                .Include(wo => wo.Product)
                .Include(wo => wo.ProductionLine);
            
            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(wo => wo.Status == status);
            }
            
            // Apply date filter
            if (!string.IsNullOrEmpty(date))
            {
                DateTime filterDate;
                
                if (date.ToLower() == "today")
                {
                    filterDate = DateTime.Today;
                }
                else if (DateTime.TryParse(date, out DateTime parsedDate))
                {
                    filterDate = parsedDate.Date;
                }
                else
                {
                    return BadRequest("Invalid date format");
                }
                
                query = query.Where(wo => 
                    wo.StartDate.Date <= filterDate && 
                    wo.EstimatedEndDate.Date >= filterDate);
            }
            
            return await query.ToListAsync();
        }

        // POST: api/orders
        [HttpPost]
        public async Task<ActionResult<WorkOrder>> CreateWorkOrder([FromBody] WorkOrderCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var product = await _context.Products.FindAsync(request.product_id);
            if (product == null)
            {
                return BadRequest("Product not found");
            }
            
            // Validate production line if provided
            ProductionLine productionLine = null;
            if (request.line_id.HasValue)
            {
                productionLine = await _context.ProductionLines.FindAsync(request.line_id);
                if (productionLine == null)
                {
                    return BadRequest("Production line not found");
                }
                
                if (productionLine.Status == "Active" || productionLine.CurrentWorkOrderId.HasValue)
                {
                    return BadRequest("Production line is not available");
                }
            }
            
            // Calculate estimated end date
            float efficiencyFactor = productionLine?.EfficiencyFactor ?? 1.0f;
            var startDate = DateTime.Now;
            var estimatedEndDate = _calculationService.CalculateEstimatedEndDate(
                startDate,
                request.product_id,
                request.quantity,
                efficiencyFactor);
            
            // Create work order
            var workOrder = new WorkOrder
            {
                ProductId = request.product_id,
                ProductionLineId = request.line_id,
                Quantity = request.quantity,
                StartDate = startDate,
                EstimatedEndDate = estimatedEndDate,
                Status = "Pending"
            };
            
            _context.WorkOrders.Add(workOrder);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWorkOrderDetails), new { id = workOrder.Id }, workOrder);
        }

        // PUT: api/orders/5/progress
        [HttpPut("{id}/progress")]
        public async Task<IActionResult> UpdateProgress(int id, [FromBody] int percent)
        {
            var workOrder = await _context.WorkOrders.FindAsync(id);
            if (workOrder == null)
            {
                return NotFound();
            }

            if (percent < 0 || percent > 100)
            {
                return BadRequest("Progress percentage must be between 0 and 100");
            }

            // If progress is 100%, complete the order
            if (percent >= 100)
            {
                workOrder.Status = "Completed";
                
                // Update production line if this was the current work order
                var productionLine = await _context.ProductionLines
                    .FirstOrDefaultAsync(pl => pl.CurrentWorkOrderId == id);
                    
                if (productionLine != null)
                {
                    productionLine.Status = "Stopped";
                    productionLine.CurrentWorkOrderId = null;
                }
            }
            
            await _context.SaveChangesAsync();
            return Ok(workOrder);
        }

        // GET: api/orders/5/details
        [HttpGet("{id}/details")]
        public async Task<ActionResult<object>> GetWorkOrderDetails(int id)
        {
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Product)
                .ThenInclude(p => p.ProductMaterials)
                .ThenInclude(pm => pm.Material)
                .Include(wo => wo.ProductionLine)
                .FirstOrDefaultAsync(wo => wo.Id == id);
                
            if (workOrder == null)
            {
                return NotFound();
            }

            // Get material requirements
            var materialRequirements = workOrder.Product.ProductMaterials
                .Select(pm => new
                {
                    MaterialId = pm.MaterialId,
                    Name = pm.Material.Name,
                    RequiredQuantity = pm.QuantityNeeded * workOrder.Quantity,
                    AvailableQuantity = pm.Material.Quantity,
                    UnitOfMeasure = pm.Material.UnitOfMeasure,
                    IsAvailable = pm.Material.Quantity >= (pm.QuantityNeeded * workOrder.Quantity)
                })
                .ToList();
            
            // Calculate production time
            float efficiencyFactor = workOrder.ProductionLine?.EfficiencyFactor ?? 1.0f;
            var productionTime = _calculationService.CalculateProductionTime(
                workOrder.ProductId, 
                workOrder.Quantity, 
                efficiencyFactor);
            
            var details = new
            {
                WorkOrder = new
                {
                    workOrder.Id,
                    ProductName = workOrder.Product.Name,
                    workOrder.Quantity,
                    workOrder.StartDate,
                    workOrder.EstimatedEndDate,
                    workOrder.Status,
                    ProductionLineName = workOrder.ProductionLine?.Name,
                    ProductionTimeHours = productionTime.TotalHours
                },
                MaterialRequirements = materialRequirements
            };

            return Ok(details);
        }
    }

    public class WorkOrderCreateRequest
    {
        public int product_id { get; set; }
        public int quantity { get; set; }
        public int? line_id { get; set; }
    }
}
