namespace GreenField.Models
{
    public class Producers
    {
        public int ProducersId { get; set; }
        public string UserId { get; set; }
        public string BusinessName { get; set; }
        public string? BusinessDescription { get; set; }
        public string BusinessBasedIn { get; set; }
        public string? Logo { get; set; }
        public ICollection<Products>? Products { get; set; }
    }
}
