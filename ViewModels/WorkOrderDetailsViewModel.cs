using MyCRM.Models;

namespace MyCRM.ViewModels
{
    public class WorkOrderDetailsViewModel
    {
        public WorkOrder WorkOrder { get; set; }
        public List<MaterialRequirementViewModel> MaterialRequirements { get; set; }
    }
}
