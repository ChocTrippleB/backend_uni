using System.ComponentModel.DataAnnotations;

namespace backend.Model
{
    /// <summary>
    /// Represents a queued payout to a seller, processed in batches every 2 days
    /// </summary>
    public class PayoutQueue
    {
        public int Id { get; set; }

        // Link to order
        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        // Seller info
        [Required]
        public Guid SellerId { get; set; }
        public User Seller { get; set; }

        [Required]
        public string SellerRecipientCode { get; set; }

        // Amount to pay
        [Required]
        public decimal Amount { get; set; }

        // Timing
        [Required]
        public DateTime QueuedAt { get; set; }  // When seller confirmed delivery

        [Required]
        public DateTime ScheduledPayoutDate { get; set; }  // When to pay (2 days later)

        public DateTime? ProcessedAt { get; set; }  // When actually paid

        // Status
        [Required]
        public PayoutStatus Status { get; set; }  // Pending, Processed, Failed

        public string? TransferReference { get; set; }  // Paystack transfer reference

        public string? FailureReason { get; set; }  // If transfer fails
    }

    /// <summary>
    /// Status of a payout in the queue
    /// </summary>
    public enum PayoutStatus
    {
        Pending = 0,      // Queued, waiting for batch processing
        Processed = 1,    // Successfully transferred to seller's bank
        Failed = 2        // Transfer failed, will be retried
    }
}
