using System.ComponentModel.DataAnnotations;

namespace backend.Model
{
    /// <summary>
    /// Stores bank account details for sellers to receive payouts
    /// </summary>
    public class BankAccount
    {
        public int Id { get; set; }

        /// <summary>
        /// User ID (seller who owns this bank account)
        /// </summary>
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        /// <summary>
        /// Bank account number
        /// </summary>
        [Required]
        public string AccountNumber { get; set; }

        /// <summary>
        /// Bank name (e.g., "Standard Bank", "FNB", "Capitec")
        /// </summary>
        [Required]
        public string BankName { get; set; }

        /// <summary>
        /// Bank code (South African bank codes)
        /// </summary>
        [Required]
        public string BankCode { get; set; }

        /// <summary>
        /// Account holder name (must match user's name for verification)
        /// </summary>
        [Required]
        public string AccountHolderName { get; set; }

        /// <summary>
        /// Account type (e.g., "savings", "current")
        /// </summary>
        [Required]
        public string AccountType { get; set; }

        /// <summary>
        /// Paystack transfer recipient code (created after bank account verification)
        /// This is used for initiating transfers
        /// </summary>
        public string? PaystackRecipientCode { get; set; }

        /// <summary>
        /// Whether this bank account has been verified by Paystack
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Whether this is the primary/active bank account for the user
        /// </summary>
        public bool IsPrimary { get; set; } = true;

        /// <summary>
        /// When this bank account was added
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this bank account was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Verification failure reason (if verification failed)
        /// </summary>
        public string? VerificationFailureReason { get; set; }
    }
}
