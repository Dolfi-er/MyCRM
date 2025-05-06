using MyCRM.Models;

namespace MyCRM.ViewModels
{
    public class ProductCreateViewModel
    {
        public Product Product { get; set; }
        public List<Material> AvailableMaterials { get; set; }
        public Dictionary<string, string> SpecificationsDict { get; set; } = new Dictionary<string, string>();
    }
}
