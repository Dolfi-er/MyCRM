using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;

namespace MyCRM.Api
{
    [Route("api/lines")]
    [ApiController]
    public class ProductionLinesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductionLinesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/lines?available=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductionLine>>> GetProductionLines([FromQuery] bool available = false)
        {
            IQueryable<ProductionLine> query = _context.ProductionLines
                .Include(pl => pl.CurrentWorkOrder)
                .ThenInclude(wo => wo != null ? wo.Product : null);
            
            if (available)
            {
                query = query.Where(pl => pl.Status == "Stopped" && pl.CurrentWorkOrderId == null);
            }
            
            return await query.ToListAsync();
        }

        // PUT: api/lines/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var productionLine = await _context.ProductionLines.FindAsync(id);
            if (productionLine == null)
            {
                return NotFound();
            }

            if (status != "Active" && status != "Stopped")
            {
                return BadRequest("Status must be either 'Active' or 'Stopped'");
            }

            productionLine.Status = status;
            
            // If stopping the line, update any in-progress work orders
            if (status == "Stopped" && productionLine.CurrentWorkOrderId.HasValue)
            {
                var workOrder = await _context.WorkOrders.FindAsync(productionLine.CurrentWorkOrderId);
                if (workOrder != null && workOrder.Status == "InProgress")
                {
                    workOrder.Status = "Pending";
                }
                
                productionLine.CurrentWorkOrderId = null;
            }
            
            await _context.SaveChangesAsync();
            return Ok(productionLine);
        }

        // GET: api/lines/5/schedule
        [HttpGet("{id}/schedule")]
        public async Task<ActionResult<IEnumerable<object>>> GetSchedule(int id)
        {
            var productionLine = await _context.ProductionLines.FindAsync(id);
            if (productionLine == null)
            {
                return NotFound();
            }

            var schedule = await _context.WorkOrders
                .Where(wo => wo.ProductionLineId == id && 
                       (wo.Status == "Pending" || wo.Status == "InProgress"))
                .Include(wo => wo.Product)
                .OrderBy(wo => wo.StartDate)
                .Select(wo => new
                {
                    WorkOrderId = wo.Id,
                    ProductName = wo.Product.Name,
                    Quantity = wo.Quantity,
                    StartDate = wo.StartDate,
                    EstimatedEndDate = wo.EstimatedEndDate,
                    Status = wo.Status,
                    IsCurrent = wo.Id == productionLine.CurrentWorkOrderId
                })
                .ToListAsync();

            return Ok(schedule);
        }
    }
}
