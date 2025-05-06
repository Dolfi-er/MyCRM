using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace MyCRM.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "jsonb")]
        public string Specifications { get; set; }

        public string Category { get; set; }

        public int MinimalStock { get; set; }

        public int ProductionTimePerUnit { get; set; }

        public ICollection<ProductMaterial> ProductMaterials { get; set; }
        public ICollection<WorkOrder> WorkOrders { get; set; }
    }
}