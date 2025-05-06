using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;
using MyCRM.ViewModels;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace MyCRM.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string category = null)
        {
            IQueryable<Product> productsQuery = _context.Products;
            
            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p => p.Category == category);
            }
            
            var products = await productsQuery.ToListAsync();
            
            // Get all categories for filter dropdown
            var categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();
            
            var viewModel = new ProductsViewModel
            {
                Products = products,
                Categories = categories,
                SelectedCategory = category
            };
            
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.ProductMaterials)
                .ThenInclude(pm => pm.Material)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        public IActionResult Create()
        {
            var materials = _context.Materials.ToList();
            var viewModel = new ProductCreateViewModel
            {
                Product = new Product(),
                AvailableMaterials = materials
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel viewModel, List<int> selectedMaterials, List<decimal> quantities)
        {
            _logger.LogInformation("Create method called with Product: {ProductName}, Materials: {MaterialCount}, Quantities: {QuantityCount}",
                viewModel.Product?.Name,
                selectedMaterials?.Count ?? 0,
                quantities?.Count ?? 0);

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                        string.Join("; ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)));
                    
                    viewModel.AvailableMaterials = await _context.Materials.ToListAsync();
                    return View(viewModel);
                }

                // Create the product
                var product = viewModel.Product;
                
                _logger.LogInformation("Creating product: {ProductName}, Category: {Category}, ProductionTime: {Time}",
                    product.Name, product.Category, product.ProductionTimePerUnit);
                
                // Convert specifications to JSON if provided
                if (viewModel.SpecificationsDict != null && viewModel.SpecificationsDict.Count > 0)
                {
                    product.Specifications = JsonConvert.SerializeObject(viewModel.SpecificationsDict);
                    _logger.LogInformation("Product specifications: {SpecCount}", viewModel.SpecificationsDict.Count);
                }
                else
                {
                    product.Specifications = "{}"; // Empty JSON object
                    _logger.LogInformation("No product specifications provided");
                }
                
                _context.Add(product);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Product created with ID: {ProductId}", product.Id);
                
                // Add product materials
                if (selectedMaterials != null && quantities != null && selectedMaterials.Count == quantities.Count)
                {
                    _logger.LogInformation("Adding {Count} materials to product", selectedMaterials.Count);
                    
                    for (int i = 0; i < selectedMaterials.Count; i++)
                    {
                        var materialId = selectedMaterials[i];
                        var quantity = quantities[i];
                        
                        _logger.LogInformation("Adding material ID: {MaterialId}, Quantity: {Quantity}", materialId, quantity);
                        
                        var productMaterial = new ProductMaterial
                        {
                            ProductId = product.Id,
                            MaterialId = materialId,
                            QuantityNeeded = quantity
                        };
                        
                        _context.ProductMaterials.Add(productMaterial);
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Product materials saved successfully");
                }
                else
                {
                    _logger.LogWarning("No materials added to product. SelectedMaterials: {Materials}, Quantities: {Quantities}",
                        selectedMaterials?.Count ?? 0,
                        quantities?.Count ?? 0);
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {Message}", ex.Message);
                ModelState.AddModelError("", $"An error occurred while creating the product: {ex.Message}");
                
                viewModel.AvailableMaterials = await _context.Materials.ToListAsync();
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.ProductMaterials)
                .ThenInclude(pm => pm.Material)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (product == null)
            {
                return NotFound();
            }
            
            var viewModel = new ProductEditViewModel
            {
                Product = product,
                AvailableMaterials = await _context.Materials.ToListAsync(),
                ProductMaterials = product.ProductMaterials.ToList()
            };
            
            // Parse specifications JSON if exists
            if (!string.IsNullOrEmpty(product.Specifications))
            {
                viewModel.SpecificationsDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(product.Specifications);
            }
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel viewModel, List<int> selectedMaterials, List<decimal> quantities)
        {
            if (id != viewModel.Product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var product = viewModel.Product;
                    
                    // Convert specifications to JSON if provided
                    if (viewModel.SpecificationsDict != null && viewModel.SpecificationsDict.Count > 0)
                    {
                        product.Specifications = JsonConvert.SerializeObject(viewModel.SpecificationsDict);
                    }
                    
                    _context.Update(product);
                    
                    // Remove existing product materials
                    var existingMaterials = _context.ProductMaterials.Where(pm => pm.ProductId == id);
                    _context.ProductMaterials.RemoveRange(existingMaterials);
                    
                    // Add updated product materials
                    if (selectedMaterials != null && quantities != null && selectedMaterials.Count == quantities.Count)
                    {
                        for (int i = 0; i < selectedMaterials.Count; i++)
                        {
                            var productMaterial = new ProductMaterial
                            {
                                ProductId = product.Id,
                                MaterialId = selectedMaterials[i],
                                QuantityNeeded = quantities[i]
                            };
                            
                            _context.ProductMaterials.Add(productMaterial);
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(viewModel.Product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            viewModel.AvailableMaterials = await _context.Materials.ToListAsync();
            return View(viewModel);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Diagnostic()
        {
            var viewModel = new ProductCreateViewModel
            {
                Product = new Product(),
                AvailableMaterials = await _context.Materials.ToListAsync(),
                SpecificationsDict = new Dictionary<string, string>()
            };
            
            return View(viewModel);
        }

        // GET: Products/SimpleCreate
        public IActionResult SimpleCreate()
        {
            _logger.LogInformation("SimpleCreate GET action called");
            return View(new Product());
        }

        // POST: Products/SimpleCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimpleCreate(Product product)
        {
            _logger.LogInformation("SimpleCreate POST action called with Product: {ProductName}", product?.Name);

            try
            {
                if (ModelState.IsValid)
                {
                    // Set default values
                    product.Specifications = "{}";
            
                    _logger.LogInformation("Creating simple product: {ProductName}", product.Name);
            
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
            
                    _logger.LogInformation("Simple product created with ID: {ProductId}", product.Id);
            
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid in SimpleCreate. Errors: {Errors}", 
                        string.Join("; ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating simple product: {Message}", ex.Message);
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }
    
            return View(product);
        }

        // Add this method to the ProductsController class
        [HttpGet]
        public IActionResult DirectCreate()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DirectCreate(Product product)
        {
            _logger.LogInformation("DirectCreate POST action called with Product: {ProductName}", product?.Name);

            try
            {
                if (ModelState.IsValid)
                {
                    // Set default values
                    product.Specifications = "{}";
            
                    _logger.LogInformation("Creating product directly: {ProductName}", product.Name);
            
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
            
                    _logger.LogInformation("Product created directly with ID: {ProductId}", product.Id);
            
                    TempData["SuccessMessage"] = $"Product '{product.Name}' created successfully with ID: {product.Id}";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid in DirectCreate. Errors: {Errors}", 
                        string.Join("; ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product directly: {Message}", ex.Message);
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }

            return View(product);
        }
    }
}
