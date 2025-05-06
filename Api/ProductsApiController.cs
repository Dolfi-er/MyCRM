using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MyCRM.Api
{
    [Route("api/products")]
    [ApiController]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsApiController> _logger;

        public ProductsApiController(ApplicationDbContext context, ILogger<ProductsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/products?category=Electronics
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] string category = null)
        {
            try
            {
                _logger.LogInformation("GetProducts API called");
                IQueryable<Product> query = _context.Products;
                
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category == category);
                }
                
                var products = await query.ToListAsync();
                _logger.LogInformation("Returning {Count} products", products.Count);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: api/products/5/materials
        [HttpGet("{id}/materials")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductMaterials(int id)
        {
            try
            {
                _logger.LogInformation("GetProductMaterials API called for product ID: {Id}", id);
                var productExists = await _context.Products.AnyAsync(p => p.Id == id);
                if (!productExists)
                {
                    _logger.LogWarning("Product not found with ID: {Id}", id);
                    return NotFound(new { error = $"Product with ID {id} not found" });
                }

                var materials = await _context.ProductMaterials
                    .Where(pm => pm.ProductId == id)
                    .Include(pm => pm.Material)
                    .Select(pm => new
                    {
                        MaterialId = pm.MaterialId,
                        Name = pm.Material.Name,
                        QuantityNeeded = pm.QuantityNeeded,
                        UnitOfMeasure = pm.Material.UnitOfMeasure,
                        AvailableQuantity = pm.Material.Quantity
                    })
                    .ToListAsync();

                _logger.LogInformation("Returning {Count} materials for product ID: {Id}", materials.Count, id);
                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product materials for ID: {Id}", id);
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct([FromBody] Product product)
        {
            _logger.LogInformation("PostProduct API called with request body: {Body}", 
                JsonSerializer.Serialize(product));
            
            try
            {
                if (product == null)
                {
                    _logger.LogWarning("Product is null");
                    return BadRequest(new { error = "Product data is null" });
                }
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                        
                    _logger.LogWarning("Invalid model state: {Errors}", string.Join("; ", errors));
                    return BadRequest(new { errors });
                }
                
                // Set default values if not provided
                if (string.IsNullOrEmpty(product.Specifications))
                {
                    product.Specifications = "{}";
                }
                
                if (string.IsNullOrEmpty(product.Description))
                {
                    product.Description = "";
                }
                
                if (string.IsNullOrEmpty(product.Category))
                {
                    product.Category = "General";
                }
                
                _logger.LogInformation("Adding product to database: {Name}", product.Name);
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Product created with ID: {Id}", product.Id);

                return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {Message}", ex.Message);
                return StatusCode(500, new { 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }
        
        // Simple test endpoint to verify API is working
        [HttpGet("test")]
        public ActionResult<object> TestApi()
        {
            return Ok(new { message = "API is working", timestamp = DateTime.Now });
        }
    }
}
