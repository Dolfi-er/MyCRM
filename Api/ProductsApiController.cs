using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;

namespace MyCRM.Api
{
    [Route("api/products")]
    [ApiController]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/products?category=Electronics
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] string category = null)
        {
            IQueryable<Product> query = _context.Products;
            
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }
            
            return await query.ToListAsync();
        }

        // GET: api/products/5/materials
        [HttpGet("{id}/materials")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductMaterials(int id)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == id);
            if (!productExists)
            {
                return NotFound();
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

            return Ok(materials);
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
        }
    }
}
