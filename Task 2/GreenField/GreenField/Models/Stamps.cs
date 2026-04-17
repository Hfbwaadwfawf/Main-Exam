namespace GreenField.Models
{
    public class Stamps
    {
        public int StampsId { get; set; }
        public string StampName { get; set; }
        public string? StampDescription { get; set; }

        public ICollection<ProductStamps>? ProductStamps { get; set; }
        public ICollection<ProducerStamps>? ProducerStamps { get; set; }
    }
}
