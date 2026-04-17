namespace GreenField.Models
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Processing,
        ReadyForCollection,
        OutForDelivery,
        Delivered,
        Collected,
        Cancelled,
        Refunded
    }

    public class Orders
    {
        public int OrdersId { get; set; }
        public string UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public bool IsDelivery { get; set; }
        public string? DeliveryAddress { get; set; }
        public decimal DeliveryFee { get; set; } = 0;
        public DateOnly? CollectionDate { get; set; }
        public bool UsedDiscount { get; set; } = false;
        public string? DiscountName { get; set; }
        public int? DiscountCodeId { get; set; }
        public DiscountCodes? DiscountCode { get; set; }
        public ICollection<OrderProducts>? OrderProducts { get; set; }
    }
}