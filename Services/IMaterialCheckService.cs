namespace MyCRM.Services
{
    public interface IMaterialCheckService
    {
        bool CheckMaterialAvailability(int productId, int quantity);
        Dictionary<int, decimal> CalculateRequiredMaterials(int productId, int quantity);
    }
}
