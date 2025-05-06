using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCRM.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        public string Description { get; set; }

        public string Specifications { get; set; }

        public string Category { get; set; }

        [Required]
        public int ProductionTimePerUnit { get; set; }

        public int MinimalStock { get; set; }

        // Navigation properties
        public virtual ICollection<ProductMaterial> ProductMaterials { get; set; }
        public virtual ICollection<WorkOrder> WorkOrders { get; set; }

        public Product()
        {
            // Initialize collections to avoid null reference exceptions
            ProductMaterials = new List<ProductMaterial>();
            WorkOrders = new List<WorkOrder>();

            // Set default values
            Description = string.Empty;
            Specifications = "{}";
            Category = "General";
            MinimalStock = 0;
        }
    }
}
