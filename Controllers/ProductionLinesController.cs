using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;
using MyCRM.ViewModels;

namespace MyCRM.Controllers
{
    public class ProductionLinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductionLinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productionLines = await _context.ProductionLines
                .Include(pl => pl.CurrentWorkOrder)
                .ThenInclude(wo => wo.Product)
                .ToListAsync();
                
            return View(productionLines);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productionLine = await _context.ProductionLines
                .Include(pl => pl.CurrentWorkOrder)
                .ThenInclude(wo => wo.Product)
                .Include(pl => pl.WorkOrders)
                .ThenInclude(wo => wo.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (productionLine == null)
            {
                return NotFound();
            }

            // Get pending work orders that could be assigned to this line
            var pendingWorkOrders = await _context.WorkOrders
                .Include(wo => wo.Product)
                .Where(wo => wo.Status == "Pending" && wo.ProductionLineId == null)
                .ToListAsync();
                
            var viewModel = new ProductionLineDetailsViewModel
            {
                ProductionLine = productionLine,
                PendingWorkOrders = pendingWorkOrders
            };
            
            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductionLine productionLine)
        {
            if (ModelState.IsValid)
            {
                productionLine.Status = "Stopped";
                productionLine.EfficiencyFactor = 1.0f;
                
                _context.Add(productionLine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(productionLine);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var productionLine = await _context.ProductionLines.FindAsync(id);
            if (productionLine == null)
            {
                return NotFound();
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
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEfficiency(int id, float efficiencyFactor)
        {
            var productionLine = await _context.ProductionLines.FindAsync(id);
            if (productionLine == null)
            {
                return NotFound();
            }

            // Ensure efficiency is within valid range
            productionLine.EfficiencyFactor = Math.Clamp(efficiencyFactor, 0.5f, 2.0f);
            
            // If there's a current work order, update its estimated end date
            if (productionLine.CurrentWorkOrderId.HasValue)
            {
                var workOrder = await _context.WorkOrders
                    .Include(wo => wo.Product)
                    .FirstOrDefaultAsync(wo => wo.Id == productionLine.CurrentWorkOrderId);
                    
                if (workOrder != null)
                {
                    // Recalculate estimated end date based on new efficiency
                    int totalMinutes = (int)Math.Ceiling(workOrder.Product.ProductionTimePerUnit * workOrder.Quantity / productionLine.EfficiencyFactor);
                    workOrder.EstimatedEndDate = workOrder.StartDate.AddMinutes(totalMinutes);
                }
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> AssignWorkOrder(int lineId, int workOrderId)
        {
            var productionLine = await _context.ProductionLines.FindAsync(lineId);
            var workOrder = await _context.WorkOrders.FindAsync(workOrderId);
            
            if (productionLine == null || workOrder == null)
            {
                return NotFound();
            }

            // Can only assign if line is not currently processing another order
            if (productionLine.CurrentWorkOrderId.HasValue)
            {
                TempData["Error"] = "This production line is already processing an order.";
                return RedirectToAction(nameof(Details), new { id = lineId });
            }

            // Assign work order to production line
            workOrder.ProductionLineId = lineId;
            workOrder.Status = "Pending";
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = lineId });
        }

        [HttpPost]
        public async Task<IActionResult> StartWorkOrder(int lineId, int workOrderId)
        {
            var productionLine = await _context.ProductionLines
                .Include(pl => pl.WorkOrders)
                .FirstOrDefaultAsync(pl => pl.Id == lineId);
                
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Product)
                .FirstOrDefaultAsync(wo => wo.Id == workOrderId);
                
            if (productionLine == null || workOrder == null)
            {
                return NotFound();
            }

            // Can only start if line is not currently processing another order
            if (productionLine.CurrentWorkOrderId.HasValue)
            {
                TempData["Error"] = "This production line is already processing an order.";
                return RedirectToAction(nameof(Details), new { id = lineId });
            }

            // Start work order
            productionLine.Status = "Active";
            productionLine.CurrentWorkOrderId = workOrderId;
            
            workOrder.Status = "InProgress";
            workOrder.StartDate = DateTime.Now;
            
            // Calculate estimated end date
            int totalMinutes = (int)Math.Ceiling(workOrder.Product.ProductionTimePerUnit * workOrder.Quantity / productionLine.EfficiencyFactor);
            workOrder.EstimatedEndDate = workOrder.StartDate.AddMinutes(totalMinutes);
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = lineId });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteWorkOrder(int lineId)
        {
            var productionLine = await _context.ProductionLines.FindAsync(lineId);
            if (productionLine == null || !productionLine.CurrentWorkOrderId.HasValue)
            {
                return NotFound();
            }

            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Product)
                .FirstOrDefaultAsync(wo => wo.Id == productionLine.CurrentWorkOrderId);
                
            if (workOrder == null)
            {
                return NotFound();
            }

            // Complete work order
            workOrder.Status = "Completed";
            
            // Update product stock (assuming we track finished products)
            var product = workOrder.Product;
            
            // Reset production line
            productionLine.Status = "Stopped";
            productionLine.CurrentWorkOrderId = null;
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = lineId });
        }
    }
}
