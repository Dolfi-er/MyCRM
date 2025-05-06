using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MyCRM.ViewModels
{
    public class WorkOrderCreateViewModel
    {
        [Required]
        public int ProductId { get; set; }
        
        public int? ProductionLineId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        public bool DeductMaterials { get; set; } = true;
        
        public SelectList Products { get; set; }
        public SelectList ProductionLines { get; set; }
    }
}
