using MyCRM.Models;

namespace MyCRM.Services
{
    public interface IProductionCalculationService
    {
        TimeSpan CalculateProductionTime(int productId, int quantity, float efficiencyFactor = 1.0f);
        DateTime CalculateEstimatedEndDate(DateTime startDate, int productId, int quantity, float efficiencyFactor = 1.0f);
        bool CheckMaterialAvailability(int productId, int quantity);
    }
}
