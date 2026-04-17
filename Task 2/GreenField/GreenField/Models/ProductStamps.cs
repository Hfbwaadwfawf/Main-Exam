namespace GreenField.Models
{
    public class ProductStamps
    {
        public int ProductStampsId { get; set; }
        public int ProductsId { get; set; }
        public int StampsId { get; set; }

        public Products Products { get; set; }
        public Stamps Stamps { get; set; }
    }
}
