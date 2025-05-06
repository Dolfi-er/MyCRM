// Material.cs
using System.ComponentModel.DataAnnotations;

namespace MyCRM.Models
{
    public class Material
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public decimal Quantity { get; set; }

        public string UnitOfMeasure { get; set; }

        public decimal MinimalStock { get; set; }

        public ICollection<ProductMaterial> ProductMaterials { get; set; }
    }
}