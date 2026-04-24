namespace GreenField.Models
{
    public class Products
    {
        public int ProductsId { get; set; }
        public int ProducersId { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public string category { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? Image { get; set; }

        public Producers? Producers { get; set; }
        public ICollection<OrderProducts>? OrderProducts { get; set; }
        public ICollection<BasketProducts>? BasketProducts { get; set; }
    }
}