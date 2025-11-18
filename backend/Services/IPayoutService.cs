using backend.Model;

namespace backend.Services
{
    public interface IPayoutService
    {
        /// <summary>
        /// Process all pending payouts scheduled for today or earlier
        /// This method should be called by a scheduled job (Hangfire) or manually
        /// </summary>
        Task<(int successCount, int failureCount, List<string> errors)> ProcessPendingPayoutsAsync();

        /// <summary>
        /// Get all pending payouts scheduled for a specific date
        /// </summary>
        Task<List<PayoutQueue>> GetPendingPayoutsByDateAsync(DateTime date);

        /// <summary>
        /// Get all payouts for a specific seller
        /// </summary>
        Task<List<PayoutQueue>> GetSellerPayoutsAsync(Guid sellerId);

        /// <summary>
        /// Get a specific payout by ID
        /// </summary>
        Task<PayoutQueue?> GetPayoutByIdAsync(int payoutId);

        /// <summary>
        /// Retry a failed payout
        /// </summary>
        Task<bool> RetryFailedPayoutAsync(int payoutId);

        /// <summary>
        /// Get payout statistics for admin dashboard
        /// </summary>
        Task<PayoutStats> GetPayoutStatsAsync();
    }

    public class PayoutStats
    {
        public int TotalPending { get; set; }
        public int TotalProcessed { get; set; }
        public int TotalFailed { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal TotalProcessedAmount { get; set; }
        public DateTime? NextScheduledDate { get; set; }
        public int PayoutsScheduledForNextDate { get; set; }
    }
}
