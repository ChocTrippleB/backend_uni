using backend.DTO;
using backend.Model;

namespace backend.Services
{
    public interface IBankAccountService
    {
        /// <summary>
        /// Add a new bank account for a user and create Paystack recipient code
        /// </summary>
        Task<(bool success, string message, BankAccountResponseDto? bankAccount)> AddBankAccountAsync(int userId, AddBankAccountDto dto);

        /// <summary>
        /// Get a user's bank account by ID
        /// </summary>
        Task<BankAccountResponseDto?> GetBankAccountByIdAsync(int userId, int bankAccountId);

        /// <summary>
        /// Get all bank accounts for a user
        /// </summary>
        Task<List<BankAccountResponseDto>> GetUserBankAccountsAsync(int userId);

        /// <summary>
        /// Get the primary bank account for a user
        /// </summary>
        Task<BankAccountResponseDto?> GetPrimaryBankAccountAsync(int userId);

        /// <summary>
        /// Update a bank account (requires re-verification)
        /// </summary>
        Task<(bool success, string message)> UpdateBankAccountAsync(int userId, int bankAccountId, AddBankAccountDto dto);

        /// <summary>
        /// Set a bank account as primary
        /// </summary>
        Task<(bool success, string message)> SetPrimaryBankAccountAsync(int userId, int bankAccountId);

        /// <summary>
        /// Delete a bank account
        /// </summary>
        Task<(bool success, string message)> DeleteBankAccountAsync(int userId, int bankAccountId);
    }
}
