using MyCRM.Models;

namespace MyCRM.ViewModels
{
    public class ProductsViewModel
    {
        public List<Product> Products { get; set; }
        public List<string> Categories { get; set; }
        public string SelectedCategory { get; set; }
    }
}
