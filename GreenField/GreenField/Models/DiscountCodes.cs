namespace GreenField.Models
{
    public class DiscountCodes
    {
        public int DiscountCodesId { get; set; }
        public string Code { get; set; }
        public decimal Percentage { get; set; }
        public bool IsActive { get; set; } = true;
        // if > 0 this is a loyalty reward — user needs this many points to unlock it
        public int PointsRequired { get; set; } = 0;

        public ICollection<Orders>? Orders { get; set; }
    }
}
