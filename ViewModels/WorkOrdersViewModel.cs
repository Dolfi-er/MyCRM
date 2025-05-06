using MyCRM.Models;

namespace MyCRM.ViewModels
{
    public class WorkOrdersViewModel
    {
        public List<WorkOrder> WorkOrders { get; set; }
        public string SelectedStatus { get; set; }
        public DateTime? SelectedDate { get; set; }
    }
}
