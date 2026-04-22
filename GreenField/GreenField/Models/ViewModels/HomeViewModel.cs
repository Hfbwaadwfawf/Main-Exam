using GreenField.Models;

namespace GreenField.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Products> FeaturedProducts { get; set; } = new();
        public List<Producers> FeaturedProducers { get; set; } = new();
        public int TotalProducers { get; set; }
        public int TotalProducts { get; set; }
    }
}