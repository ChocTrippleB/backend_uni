using backend.Data;
using backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace backend.Services
{
    public class PayoutService : IPayoutService
    {
        private readonly AppDbContext _context;
        private readonly IPaystackService _paystackService;
        private readonly ILogger<PayoutService> _logger;
        private readonly IConfiguration _configuration;

        public PayoutService(
            AppDbContext context,
            IPaystackService paystackService,
            ILogger<PayoutService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _paystackService = paystackService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Process all pending payouts scheduled for today or earlier
        /// Returns (successCount, failureCount, errorMessages)
        /// </summary>
        public async Task<(int successCount, int failureCount, List<string> errors)> ProcessPendingPayoutsAsync()
        {
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var errors = new List<string>();
            int successCount = 0;
            int failureCount = 0;

            try
            {
                // Safety check: Don't auto-process if Paystack is in test mode
                var paystackSecretKey = _configuration["Paystack:SecretKey"];
                if (!string.IsNullOrEmpty(paystackSecretKey) && paystackSecretKey.Contains("test_"))
                {
                    _logger.LogWarning("⚠️ Skipping automatic payout processing - Paystack is in test mode. Use manual API endpoint to test.");
                    return (0, 0, new List<string> { "Automatic processing disabled in test mode. Use manual API endpoint instead." });
                }

                // Get all pending payouts scheduled for today or earlier
                var pendingPayouts = await _context.PayoutQueue
                    .Include(p => p.Order)
                    .Include(p => p.Seller)
                    .Where(p => p.Status == PayoutStatus.Pending &&
                               p.ScheduledPayoutDate <= today)
                    .OrderBy(p => p.QueuedAt)
                    .ToListAsync();

                _logger.LogInformation($"Processing {pendingPayouts.Count} pending payouts for {today:yyyy-MM-dd}");

                if (pendingPayouts.Count == 0)
                {
                    _logger.LogInformation("No pending payouts to process");
                    return (0, 0, new List<string> { "No pending payouts to process" });
                }

                // Group payouts by seller to create batch transfers
                var payoutsBySeller = pendingPayouts.GroupBy(p => p.SellerId);

                foreach (var sellerPayouts in payoutsBySeller)
                {
                    var sellerId = sellerPayouts.Key;
                    var seller = sellerPayouts.First().Seller;

                    _logger.LogInformation($"Processing {sellerPayouts.Count()} payouts for seller {sellerId}");

                    // Process each payout individually (Paystack doesn't support batch transfers in one API call)
                    // But we batch them by date to reduce overall transfer frequency
                    foreach (var payout in sellerPayouts)
                    {
                        try
                        {
                            // Generate unique reference for this transfer
                            var transferReference = $"TRANSFER-{payout.Id}-{DateTime.UtcNow.Ticks}";

                            // Initiate transfer via Paystack
                            var transferSuccess = await _paystackService.InitiateTransferAsync(
                                payout.SellerRecipientCode,
                                payout.Amount,
                                transferReference);

                            if (transferSuccess)
                            {
                                // Update payout status to Processed
                                payout.Status = PayoutStatus.Processed;
                                payout.ProcessedAt = DateTime.UtcNow;
                                payout.TransferReference = transferReference;

                                // Update order status to Completed
                                var order = payout.Order;
                                if (order != null)
                                {
                                    order.Status = OrderStatus.Completed;
                                }

                                successCount++;
                                _logger.LogInformation($"✅ Payout {payout.Id} processed successfully. Transfer: {payout.TransferReference}");
                            }
                            else
                            {
                                // Mark as failed
                                payout.Status = PayoutStatus.Failed;
                                payout.FailureReason = "Transfer failed - Paystack API returned error";

                                failureCount++;
                                var errorMsg = $"Payout {payout.Id} failed: {payout.FailureReason}";
                                errors.Add(errorMsg);
                                _logger.LogError(errorMsg);
                            }

                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            // Mark payout as failed
                            payout.Status = PayoutStatus.Failed;
                            payout.FailureReason = ex.Message;

                            failureCount++;
                            var errorMsg = $"Exception processing payout {payout.Id}: {ex.Message}";
                            errors.Add(errorMsg);
                            _logger.LogError(ex, errorMsg);

                            await _context.SaveChangesAsync();
                        }
                    }
                }

                _logger.LogInformation($"Batch processing complete: {successCount} succeeded, {failureCount} failed");
                return (successCount, failureCount, errors);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Fatal error processing payouts: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                return (successCount, failureCount, new List<string> { errorMsg });
            }
        }

        public async Task<List<PayoutQueue>> GetPendingPayoutsByDateAsync(DateTime date)
        {
            // Ensure date is in UTC for PostgreSQL comparison
            var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var nextDay = utcDate.AddDays(1);

            return await _context.PayoutQueue
                .Include(p => p.Order)
                .Include(p => p.Seller)
                .Where(p => p.Status == PayoutStatus.Pending &&
                           p.ScheduledPayoutDate >= utcDate &&
                           p.ScheduledPayoutDate < nextDay)
                .OrderBy(p => p.QueuedAt)
                .ToListAsync();
        }

        public async Task<List<PayoutQueue>> GetSellerPayoutsAsync(int sellerId)
        {
            return await _context.PayoutQueue
                .Include(p => p.Order)
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.QueuedAt)
                .ToListAsync();
        }

        public async Task<PayoutQueue?> GetPayoutByIdAsync(int payoutId)
        {
            return await _context.PayoutQueue
                .Include(p => p.Order)
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == payoutId);
        }

        public async Task<bool> RetryFailedPayoutAsync(int payoutId)
        {
            var payout = await GetPayoutByIdAsync(payoutId);

            if (payout == null || payout.Status != PayoutStatus.Failed)
            {
                return false;
            }

            try
            {
                // Reset status to pending and reschedule for next payout date
                payout.Status = PayoutStatus.Pending;
                payout.FailureReason = null;
                payout.ScheduledPayoutDate = GetNextPayoutDate();

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Payout {payoutId} rescheduled for {payout.ScheduledPayoutDate:yyyy-MM-dd}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrying payout {payoutId}");
                return false;
            }
        }

        public async Task<PayoutStats> GetPayoutStatsAsync()
        {
            var pendingPayouts = await _context.PayoutQueue
                .Where(p => p.Status == PayoutStatus.Pending)
                .ToListAsync();

            var processedPayouts = await _context.PayoutQueue
                .Where(p => p.Status == PayoutStatus.Processed)
                .ToListAsync();

            var failedPayouts = await _context.PayoutQueue
                .Where(p => p.Status == PayoutStatus.Failed)
                .ToListAsync();

            var nextDate = GetNextPayoutDate();
            var nextDateCount = await _context.PayoutQueue
                .Where(p => p.Status == PayoutStatus.Pending &&
                           p.ScheduledPayoutDate.Date == nextDate.Date)
                .CountAsync();

            return new PayoutStats
            {
                TotalPending = pendingPayouts.Count,
                TotalProcessed = processedPayouts.Count,
                TotalFailed = failedPayouts.Count,
                TotalPendingAmount = pendingPayouts.Sum(p => p.Amount),
                TotalProcessedAmount = processedPayouts.Sum(p => p.Amount),
                NextScheduledDate = nextDate,
                PayoutsScheduledForNextDate = nextDateCount
            };
        }

        /// <summary>
        /// Calculate next payout date based on Mon/Wed/Fri schedule
        /// </summary>
        private DateTime GetNextPayoutDate(DateTime? fromDate = null)
        {
            var today = DateTime.SpecifyKind((fromDate ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
            var dayOfWeek = today.DayOfWeek;

            // Payout days: Monday, Wednesday, Friday
            switch (dayOfWeek)
            {
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                case DayOfWeek.Monday:
                    return GetNextDayOfWeek(today, DayOfWeek.Monday);
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                    return GetNextDayOfWeek(today, DayOfWeek.Wednesday);
                case DayOfWeek.Thursday:
                case DayOfWeek.Friday:
                    return GetNextDayOfWeek(today, DayOfWeek.Friday);
                default:
                    return DateTime.SpecifyKind(today.AddDays(2), DateTimeKind.Utc);
            }
        }

        private DateTime GetNextDayOfWeek(DateTime current, DayOfWeek targetDay)
        {
            int daysUntilTarget = ((int)targetDay - (int)current.DayOfWeek + 7) % 7;
            if (daysUntilTarget == 0) daysUntilTarget = 7; // If today is target day, schedule for next week
            return DateTime.SpecifyKind(current.AddDays(daysUntilTarget), DateTimeKind.Utc);
        }
    }
}
