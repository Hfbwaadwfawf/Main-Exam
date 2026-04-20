namespace GreenField.Models
{
    public class LoyaltyPoints
    {
        public int LoyaltyPointsId { get; set; }
        public string UserId { get; set; }
        public int Points { get; set; } = 0;
    }
}
