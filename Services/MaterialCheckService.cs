using Microsoft.EntityFrameworkCore;
using MyCRM.Models;

namespace MyCRM.Services
{
    public class MaterialCheckService : IMaterialCheckService
    {
        private readonly ApplicationDbContext _context;

        public MaterialCheckService(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CheckMaterialAvailability(int productId, int quantity)
        {
            var requiredMaterials = CalculateRequiredMaterials(productId, quantity);
            
            foreach (var materialEntry in requiredMaterials)
            {
                var materialId = materialEntry.Key;
                var requiredQuantity = materialEntry.Value;
                
                var material = _context.Materials.Find(materialId);
                if (material == null || material.Quantity < requiredQuantity)
                {
                    return false;
                }
            }
            
            return true;
        }

        public Dictionary<int, decimal> CalculateRequiredMaterials(int productId, int quantity)
        {
            var productMaterials = _context.ProductMaterials
                .Where(pm => pm.ProductId == productId)
                .Include(pm => pm.Material)
                .ToList();
            
            var requiredMaterials = new Dictionary<int, decimal>();
            
            foreach (var pm in productMaterials)
            {
                requiredMaterials[pm.MaterialId] = pm.QuantityNeeded * quantity;
            }
            
            return requiredMaterials;
        }
    }
}
