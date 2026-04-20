namespace GreenField.Models
{
    public class BasketProducts
    {
        public int BasketProductsId { get; set; }
        public int BasketId { get; set; }
        public int ProductsId { get; set; }
        public int Quantity { get; set; }
        public Basket Basket { get; set; }
        public Products Products { get; set; }
    }
}
