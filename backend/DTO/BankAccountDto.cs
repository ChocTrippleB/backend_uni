using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    /// <summary>
    /// DTO for adding/updating bank account details
    /// </summary>
    public class AddBankAccountDto
    {
        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string BankName { get; set; }

        [Required]
        [StringLength(20)]
        public string BankCode { get; set; }

        [Required]
        [StringLength(200)]
        public string AccountHolderName { get; set; }

        [Required]
        [StringLength(20)]
        public string AccountType { get; set; } // "savings" or "current"
    }

    /// <summary>
    /// Response DTO for bank account information
    /// </summary>
    public class BankAccountResponseDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
        public string BankCode { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountType { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPrimary { get; set; }
        public string? PaystackRecipientCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? VerificationFailureReason { get; set; }
    }
}
