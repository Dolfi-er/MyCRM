using MyCRM.Models;

namespace MyCRM.ViewModels
{
    public class ProductionLineDetailsViewModel
    {
        public ProductionLine ProductionLine { get; set; }
        public List<WorkOrder> PendingWorkOrders { get; set; }
    }
}
