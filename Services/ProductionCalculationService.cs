using MyCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace MyCRM.Services
{
    public class ProductionCalculationService : IProductionCalculationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMaterialCheckService _materialCheckService;

        public ProductionCalculationService(ApplicationDbContext context, IMaterialCheckService materialCheckService)
        {
            _context = context;
            _materialCheckService = materialCheckService;
        }

        public TimeSpan CalculateProductionTime(int productId, int quantity, float efficiencyFactor = 1.0f)
        {
            var product = _context.Products.Find(productId);
            if (product == null)
                throw new ArgumentException("Product not found", nameof(productId));

            // Calculate total minutes
            int totalMinutes = (int)Math.Ceiling(product.ProductionTimePerUnit * quantity / efficiencyFactor);
            
            return TimeSpan.FromMinutes(totalMinutes);
        }

        public DateTime CalculateEstimatedEndDate(DateTime startDate, int productId, int quantity, float efficiencyFactor = 1.0f)
        {
            var productionTime = CalculateProductionTime(productId, quantity, efficiencyFactor);
            return startDate.Add(productionTime);
        }

        public bool CheckMaterialAvailability(int productId, int quantity)
        {
            return _materialCheckService.CheckMaterialAvailability(productId, quantity);
        }
    }
}
