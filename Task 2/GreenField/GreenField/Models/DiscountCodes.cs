namespace GreenField.Models
{
    public class DiscountCodes
    {
        public int DiscountCodesId { get; set; }
        public string Code { get; set; }
        public decimal Percentage { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Orders>? Orders { get; set; }
    }
}
