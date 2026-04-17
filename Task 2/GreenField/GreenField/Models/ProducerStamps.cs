namespace GreenField.Models
{
    public class ProducerStamps
    {
        public int ProducerStampsId { get; set; }
        public int ProducersId { get; set; }
        public int StampsId { get; set; }

        public Producers Producers { get; set; }
        public Stamps Stamps { get; set; }
    }
}
