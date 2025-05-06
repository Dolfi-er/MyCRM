using MyCRM.Models;

namespace MyCRM.ViewModels
{
    public class MaterialRequirementViewModel
    {
        public Material Material { get; set; }
        public decimal RequiredQuantity { get; set; }
        public bool IsAvailable { get; set; }
    }
}
