using backend.DTO;
using backend.Model;

namespace backend.Services
{
    public interface IBankAccountService
    {
        /// <summary>
        /// Add a new bank account for a user and create Paystack recipient code
        /// </summary>
        Task<(bool success, string message, BankAccountResponseDto? bankAccount)> AddBankAccountAsync(Guid userId, AddBankAccountDto dto);

        /// <summary>
        /// Get a user's bank account by ID
        /// </summary>
        Task<BankAccountResponseDto?> GetBankAccountByIdAsync(Guid userId, int bankAccountId);

        /// <summary>
        /// Get all bank accounts for a user
        /// </summary>
        Task<List<BankAccountResponseDto>> GetUserBankAccountsAsync(Guid userId);

        /// <summary>
        /// Get the primary bank account for a user
        /// </summary>
        Task<BankAccountResponseDto?> GetPrimaryBankAccountAsync(Guid userId);

        /// <summary>
        /// Update a bank account (requires re-verification)
        /// </summary>
        Task<(bool success, string message)> UpdateBankAccountAsync(Guid userId, int bankAccountId, AddBankAccountDto dto);

        /// <summary>
        /// Set a bank account as primary
        /// </summary>
        Task<(bool success, string message)> SetPrimaryBankAccountAsync(Guid userId, int bankAccountId);

        /// <summary>
        /// Delete a bank account
        /// </summary>
        Task<(bool success, string message)> DeleteBankAccountAsync(Guid userId, int bankAccountId);
    }
}
