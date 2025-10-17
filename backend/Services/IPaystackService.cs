using backend.DTO;

namespace backend.Services
{
    public interface IPaystackService
    {
        /// <summary>
        /// Initialize a payment transaction with Paystack
        /// </summary>
        Task<PaystackInitializeResponse?> InitializePaymentAsync(string email, decimal amount, string reference);

        /// <summary>
        /// Verify a payment transaction with Paystack
        /// </summary>
        Task<PaystackVerifyResponse?> VerifyPaymentAsync(string reference);

        /// <summary>
        /// Create a transfer recipient (seller account) for payouts
        /// </summary>
        Task<string?> CreateTransferRecipientAsync(string accountNumber, string bankCode, string accountName);

        /// <summary>
        /// Initiate a payout to seller
        /// </summary>
        Task<bool> InitiateTransferAsync(string recipientCode, decimal amount, string reference);
    }
}
