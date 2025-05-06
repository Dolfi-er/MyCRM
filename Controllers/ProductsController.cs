using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;
using MyCRM.ViewModels;
using System.Text.Json;

namespace MyCRM.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
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
            if (ModelState.IsValid)
            {
                // Create the product
                var product = viewModel.Product;
                
                // Convert specifications to JSON if provided
                if (viewModel.SpecificationsDict != null && viewModel.SpecificationsDict.Count > 0)
                {
                    product.Specifications = JsonSerializer.Serialize(viewModel.SpecificationsDict);
                }
                
                _context.Add(product);
                await _context.SaveChangesAsync();
                
                // Add product materials
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
                    
                    await _context.SaveChangesAsync();
                }
                
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            viewModel.AvailableMaterials = await _context.Materials.ToListAsync();
            return View(viewModel);
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
                viewModel.SpecificationsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(product.Specifications);
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
                        product.Specifications = JsonSerializer.Serialize(viewModel.SpecificationsDict);
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
    }
}
