namespace GreenField.Models
{
    public class OrderProducts
    {
        public int OrderProductsId { get; set; }
        public int ProductsId { get; set; } //Links to a product
        public int OrdersId { get; set; } //Links to an order
        public int Quantity { get; set; } //Keeps track of the quantity
        public Products Products { get; set; }
        public Orders Orders { get; set; }
    }
}
