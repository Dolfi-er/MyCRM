using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;
using MyCRM.Services;
using MyCRM.ViewModels;

namespace MyCRM.Controllers
{
    public class WorkOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductionCalculationService _calculationService;
        private readonly IMaterialCheckService _materialCheckService;

        public WorkOrdersController(
            ApplicationDbContext context, 
            IProductionCalculationService calculationService,
            IMaterialCheckService materialCheckService)
        {
            _context = context;
            _calculationService = calculationService;
            _materialCheckService = materialCheckService;
        }

        public async Task<IActionResult> Index(string status = null, DateTime? date = null)
        {
            IQueryable<WorkOrder> ordersQuery = _context.WorkOrders
                .Include(wo => wo.Product)
                .Include(wo => wo.ProductionLine);
            
            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(wo => wo.Status == status);
            }
            
            // Apply date filter
            if (date.HasValue)
            {
                var filterDate = date.Value.Date;
                ordersQuery = ordersQuery.Where(wo => 
                    wo.StartDate.Date <= filterDate && 
                    wo.EstimatedEndDate.Date >= filterDate);
            }
            
            var workOrders = await ordersQuery.ToListAsync();
            
            var viewModel = new WorkOrdersViewModel
            {
                WorkOrders = workOrders,
                SelectedStatus = status,
                SelectedDate = date
            };
            
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Product)
                .ThenInclude(p => p.ProductMaterials)
                .ThenInclude(pm => pm.Material)
                .Include(wo => wo.ProductionLine)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (workOrder == null)
            {
                return NotFound();
            }

            // Calculate required materials
            var requiredMaterials = _materialCheckService.CalculateRequiredMaterials(workOrder.ProductId, workOrder.Quantity);
            var materialDetails = new List<MaterialRequirementViewModel>();
            
            foreach (var entry in requiredMaterials)
            {
                var material = await _context.Materials.FindAsync(entry.Key);
                if (material != null)
                {
                    materialDetails.Add(new MaterialRequirementViewModel
                    {
                        Material = material,
                        RequiredQuantity = entry.Value,
                        IsAvailable = material.Quantity >= entry.Value
                    });
                }
            }
            
            var viewModel = new WorkOrderDetailsViewModel
            {
                WorkOrder = workOrder,
                MaterialRequirements = materialDetails
            };
            
            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var products = await _context.Products.ToListAsync();
            var productionLines = await _context.ProductionLines
                .Where(pl => pl.Status == "Stopped")
                .ToListAsync();
                
            var viewModel = new WorkOrderCreateViewModel
            {
                Products = new SelectList(products, "Id", "Name"),
                ProductionLines = new SelectList(productionLines, "Id", "Name"),
                StartDate = DateTime.Today
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkOrderCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check material availability
                bool materialsAvailable = _materialCheckService.CheckMaterialAvailability(
                    viewModel.ProductId, 
                    viewModel.Quantity);
                    
                if (!materialsAvailable)
                {
                    ModelState.AddModelError("", "Insufficient materials available for this order.");
                    
                    // Repopulate dropdown lists
                    viewModel.Products = new SelectList(await _context.Products.ToListAsync(), "Id", "Name");
                    viewModel.ProductionLines = new SelectList(
                        await _context.ProductionLines.Where(pl => pl.Status == "Stopped").ToListAsync(), 
                        "Id", "Name");
                        
                    return View(viewModel);
                }
                
                // Calculate estimated end date
                float efficiencyFactor = 1.0f;
                if (viewModel.ProductionLineId.HasValue)
                {
                    var productionLine = await _context.ProductionLines.FindAsync(viewModel.ProductionLineId);
                    if (productionLine != null)
                    {
                        efficiencyFactor = productionLine.EfficiencyFactor;
                    }
                }
                
                var estimatedEndDate = _calculationService.CalculateEstimatedEndDate(
                    viewModel.StartDate,
                    viewModel.ProductId,
                    viewModel.Quantity,
                    efficiencyFactor);
                
                // Create work order
                var workOrder = new WorkOrder
                {
                    ProductId = viewModel.ProductId,
                    ProductionLineId = viewModel.ProductionLineId,
                    Quantity = viewModel.Quantity,
                    StartDate = viewModel.StartDate,
                    EstimatedEndDate = estimatedEndDate,
                    Status = "Pending"
                };
                
                _context.WorkOrders.Add(workOrder);
                
                // Deduct materials from inventory
                if (viewModel.DeductMaterials)
                {
                    var requiredMaterials = _materialCheckService.CalculateRequiredMaterials(
                        viewModel.ProductId, 
                        viewModel.Quantity);
                        
                    foreach (var entry in requiredMaterials)
                    {
                        var material = await _context.Materials.FindAsync(entry.Key);
                        if (material != null)
                        {
                            material.Quantity -= entry.Value;
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            viewModel.Products = new SelectList(await _context.Products.ToListAsync(), "Id", "Name");
            viewModel.ProductionLines = new SelectList(
                await _context.ProductionLines.Where(pl => pl.Status == "Stopped").ToListAsync(), 
                "Id", "Name");
                
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var workOrder = await _context.WorkOrders.FindAsync(id);
            if (workOrder == null)
            {
                return NotFound();
            }

            workOrder.Status = "Cancelled";
            
            // If this work order is currently being processed, update the production line
            var productionLine = await _context.ProductionLines
                .FirstOrDefaultAsync(pl => pl.CurrentWorkOrderId == id);
                
            if (productionLine != null)
            {
                productionLine.Status = "Stopped";
                productionLine.CurrentWorkOrderId = null;
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProgress(int id, int progressPercent)
        {
            var workOrder = await _context.WorkOrders.FindAsync(id);
            if (workOrder == null)
            {
                return NotFound();
            }

            // If progress is 100%, complete the order
            if (progressPercent >= 100)
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
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
