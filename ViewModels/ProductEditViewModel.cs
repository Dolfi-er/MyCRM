using MyCRM.Models;

namespace MyCRM.ViewModels
{
    public class ProductEditViewModel
    {
        public Product Product { get; set; }
        public List<Material> AvailableMaterials { get; set; }
        public List<ProductMaterial> ProductMaterials { get; set; }
        public Dictionary<string, string> SpecificationsDict { get; set; } = new Dictionary<string, string>();
    }
}
