using System.ComponentModel.DataAnnotations;

namespace backend.Model
{
    public enum OrderStatus
    {
        Pending = 0,           // Order created, awaiting payment
        Paid = 1,              // Payment received (transitional state)
        AwaitingRelease = 2,   // Paid, waiting for buyer's 6-digit code
        AwaitingPayout = 3,    // Code verified, queued for batch payout
        Completed = 4,         // Payout processed, order complete
        Refunded = 5,          // Payment refunded to buyer
        Cancelled = 6          // Order cancelled
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public int BuyerId { get; set; }
        public User Buyer { get; set; } = null!;

        [Required]
        public int SellerId { get; set; }
        public User Seller { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        /// <summary>
        /// Paystack payment reference (unique transaction ID from Paystack)
        /// </summary>
        public string? PaymentReference { get; set; }

        /// <summary>
        /// Current status of the order
        /// </summary>
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        /// <summary>
        /// 6-digit unique release code shown to buyer after payment
        /// Buyer gives this to seller after receiving item
        /// </summary>
        [MaxLength(6)]
        public string? ReleaseCode { get; set; }

        /// <summary>
        /// When the order was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When payment was confirmed
        /// </summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// When funds were released to seller
        /// </summary>
        public DateTime? ReleasedAt { get; set; }

        /// <summary>
        /// Optional: Auto-release after this time (e.g., 72 hours)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Number of failed release code attempts (for security)
        /// </summary>
        public int FailedReleaseAttempts { get; set; } = 0;

        /// <summary>
        /// Shipping/delivery address
        /// </summary>
        public string? ShippingAddress { get; set; }

        /// <summary>
        /// Buyer's phone number for meetup coordination
        /// </summary>
        public string? BuyerPhone { get; set; }

        /// <summary>
        /// Any special notes about the order
        /// </summary>
        public string? Notes { get; set; }
    }
}
