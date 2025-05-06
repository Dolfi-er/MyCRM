using Microsoft.AspNetCore.Mvc;
using MyCRM.Models;
using MyCRM.Services;

namespace MyCRM.Api
{
    [Route("api/calculate")]
    [ApiController]
    public class CalculationApiController : ControllerBase
    {
        private readonly IProductionCalculationService _calculationService;
        private readonly IMaterialCheckService _materialCheckService;

        public CalculationApiController(
            IProductionCalculationService calculationService,
            IMaterialCheckService materialCheckService)
        {
            _calculationService = calculationService;
            _materialCheckService = materialCheckService;
        }

        // POST: api/calculate/production
        [HttpPost("production")]
        public ActionResult<object> CalculateProduction([FromBody] ProductionCalculationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                var productionTime = _calculationService.CalculateProductionTime(
                    request.product_id, 
                    request.quantity, 
                    request.efficiency_factor ?? 1.0f);
                
                var estimatedEndDate = DateTime.Now.Add(productionTime);
                
                // Check material availability
                var materialsAvailable = _materialCheckService.CheckMaterialAvailability(
                    request.product_id, 
                    request.quantity);
                
                var requiredMaterials = _materialCheckService.CalculateRequiredMaterials(
                    request.product_id, 
                    request.quantity);
                
                return Ok(new
                {
                    production_time_minutes = productionTime.TotalMinutes,
                    production_time_hours = productionTime.TotalHours,
                    estimated_end_date = estimatedEndDate,
                    materials_available = materialsAvailable,
                    required_materials = requiredMaterials
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class ProductionCalculationRequest
    {
        public int product_id { get; set; }
        public int quantity { get; set; }
        public float? efficiency_factor { get; set; }
    }
}
