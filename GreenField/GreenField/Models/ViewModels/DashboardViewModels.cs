using GreenField.Models;

namespace GreenField.Models.ViewModels
{
    public class StandardDashboardViewModel
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public int Points { get; set; }
        public int PointsToNextReward { get; set; }
        public List<Orders> Orders { get; set; } = new();
    }

    public class ProducerDashboardViewModel
    {
        public string UserName { get; set; } = "";
        public Producers Producer { get; set; } = null!;
        public List<Products> Products { get; set; } = new();
        public List<Orders> Orders { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public string UserName { get; set; } = "";
        public List<Producers> AllProducers { get; set; } = new();
        public Producers? SelectedProducer { get; set; }
        public int? FilterProducerId { get; set; }
        public List<Products> Products { get; set; } = new();
        public List<Orders> Orders { get; set; } = new();
    }
}
