// WorkOrder.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace MyCRM.Models
{
    public class WorkOrder
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int? ProductionLineId { get; set; }
        public ProductionLine ProductionLine { get; set; }

        public int Quantity { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EstimatedEndDate { get; set; }

        public string Status { get; set; } // "Pending", "InProgress", "Completed", "Cancelled"
    }
}