using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCRM.Models;

namespace MyCRM.Api
{
    [Route("api/materials")]
    [ApiController]
    public class MaterialsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MaterialsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/materials?low_stock=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterials([FromQuery] bool low_stock = false)
        {
            IQueryable<Material> query = _context.Materials;
            
            if (low_stock)
            {
                query = query.Where(m => m.Quantity <= m.MinimalStock);
            }
            
            return await query.ToListAsync();
        }

        // POST: api/materials
        [HttpPost]
        public async Task<ActionResult<Material>> PostMaterial(Material material)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            _context.Materials.Add(material);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMaterials), new { id = material.Id }, material);
        }

        // PUT: api/materials/5/stock
        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] decimal amount)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            material.Quantity += amount;
            await _context.SaveChangesAsync();

            return Ok(material);
        }
    }
}
