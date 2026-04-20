namespace GreenField.Models
{
    public class Basket
    {
        public int BasketId { get; set; }
        public bool Status { get; set; }
        public DateTime BasketCreatedAt { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } //Links each Basket to a User
        public ICollection<BasketProducts>? BasketProducts { get; set; }
    }
}
