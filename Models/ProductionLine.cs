using System.ComponentModel.DataAnnotations;

namespace MyCRM.Models
{
    public class ProductionLine
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Status { get; set; } // "Active" or "Stopped"

        [Range(0.5, 2.0)]
        public float EfficiencyFactor { get; set; }

        public int? CurrentWorkOrderId { get; set; }
        public WorkOrder CurrentWorkOrder { get; set; }

        public ICollection<WorkOrder> WorkOrders { get; set; }
    }
}