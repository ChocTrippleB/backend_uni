using backend.Data;
using backend.DTO;
using backend.Model;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly AppDbContext _context;
        private readonly IPaystackService _paystackService;
        private readonly ILogger<BankAccountService> _logger;

        public BankAccountService(
            AppDbContext context,
            IPaystackService paystackService,
            ILogger<BankAccountService> logger)
        {
            _context = context;
            _paystackService = paystackService;
            _logger = logger;
        }

        public async Task<(bool success, string message, BankAccountResponseDto? bankAccount)> AddBankAccountAsync(
            int userId,
            AddBankAccountDto dto)
        {
            try
            {
                // Get user details
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return (false, "User not found", null);

                // Check if user already has a bank account
                var existingBankAccount = await _context.BankAccounts
                    .FirstOrDefaultAsync(b => b.UserId == userId);

                bool isPrimary = existingBankAccount == null; // First account is primary

                // Create Paystack transfer recipient
                _logger.LogInformation($"Creating Paystack transfer recipient for user {userId}");
                var recipientCode = await _paystackService.CreateTransferRecipientAsync(
                    dto.AccountNumber,
                    dto.BankCode,
                    dto.AccountHolderName);

                if (string.IsNullOrEmpty(recipientCode))
                {
                    _logger.LogError($"Failed to create Paystack recipient for user {userId}");
                    return (false, "Failed to verify bank account with Paystack. Please check your details.", null);
                }

                // Create bank account record
                var bankAccount = new BankAccount
                {
                    UserId = userId,
                    AccountNumber = dto.AccountNumber,
                    BankName = dto.BankName,
                    BankCode = dto.BankCode,
                    AccountHolderName = dto.AccountHolderName,
                    AccountType = dto.AccountType,
                    PaystackRecipientCode = recipientCode,
                    IsVerified = true,
                    IsPrimary = isPrimary,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BankAccounts.Add(bankAccount);

                // Update User's PaystackRecipientCode if this is primary
                if (isPrimary)
                {
                    user.PaystackRecipientCode = recipientCode;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bank account added successfully for user {userId}. Recipient code: {recipientCode}");

                var response = MapToDto(bankAccount);
                return (true, "Bank account added successfully", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding bank account for user {userId}");
                return (false, $"Error adding bank account: {ex.Message}", null);
            }
        }

        public async Task<BankAccountResponseDto?> GetBankAccountByIdAsync(int userId, int bankAccountId)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.UserId == userId);

            return bankAccount != null ? MapToDto(bankAccount) : null;
        }

        public async Task<List<BankAccountResponseDto>> GetUserBankAccountsAsync(int userId)
        {
            var bankAccounts = await _context.BankAccounts
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.IsPrimary)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bankAccounts.Select(MapToDto).ToList();
        }

        public async Task<BankAccountResponseDto?> GetPrimaryBankAccountAsync(int userId)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.UserId == userId && b.IsPrimary);

            return bankAccount != null ? MapToDto(bankAccount) : null;
        }

        public async Task<(bool success, string message)> UpdateBankAccountAsync(
            int userId,
            int bankAccountId,
            AddBankAccountDto dto)
        {
            try
            {
                var bankAccount = await _context.BankAccounts
                    .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.UserId == userId);

                if (bankAccount == null)
                    return (false, "Bank account not found");

                // Create new Paystack recipient (updating requires new recipient)
                _logger.LogInformation($"Updating bank account {bankAccountId} for user {userId}");
                var recipientCode = await _paystackService.CreateTransferRecipientAsync(
                    dto.AccountNumber,
                    dto.BankCode,
                    dto.AccountHolderName);

                if (string.IsNullOrEmpty(recipientCode))
                {
                    _logger.LogError($"Failed to create Paystack recipient for updated bank account {bankAccountId}");
                    return (false, "Failed to verify updated bank account with Paystack. Please check your details.");
                }

                // Update bank account details
                bankAccount.AccountNumber = dto.AccountNumber;
                bankAccount.BankName = dto.BankName;
                bankAccount.BankCode = dto.BankCode;
                bankAccount.AccountHolderName = dto.AccountHolderName;
                bankAccount.AccountType = dto.AccountType;
                bankAccount.PaystackRecipientCode = recipientCode;
                bankAccount.IsVerified = true;
                bankAccount.UpdatedAt = DateTime.UtcNow;
                bankAccount.VerificationFailureReason = null;

                // Update User's recipient code if this is primary
                if (bankAccount.IsPrimary)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.PaystackRecipientCode = recipientCode;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bank account {bankAccountId} updated successfully");
                return (true, "Bank account updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating bank account {bankAccountId}");
                return (false, $"Error updating bank account: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> SetPrimaryBankAccountAsync(int userId, int bankAccountId)
        {
            try
            {
                var bankAccount = await _context.BankAccounts
                    .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.UserId == userId);

                if (bankAccount == null)
                    return (false, "Bank account not found");

                if (!bankAccount.IsVerified)
                    return (false, "Cannot set unverified bank account as primary");

                // Unset current primary
                var currentPrimary = await _context.BankAccounts
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.IsPrimary);

                if (currentPrimary != null)
                {
                    currentPrimary.IsPrimary = false;
                }

                // Set new primary
                bankAccount.IsPrimary = true;

                // Update User's recipient code
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.PaystackRecipientCode = bankAccount.PaystackRecipientCode;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bank account {bankAccountId} set as primary for user {userId}");
                return (true, "Primary bank account updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting primary bank account {bankAccountId}");
                return (false, $"Error setting primary bank account: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeleteBankAccountAsync(int userId, int bankAccountId)
        {
            try
            {
                var bankAccount = await _context.BankAccounts
                    .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.UserId == userId);

                if (bankAccount == null)
                    return (false, "Bank account not found");

                bool wasPrimary = bankAccount.IsPrimary;

                _context.BankAccounts.Remove(bankAccount);

                // If deleted account was primary, set another one as primary
                if (wasPrimary)
                {
                    var nextBankAccount = await _context.BankAccounts
                        .Where(b => b.UserId == userId)
                        .OrderByDescending(b => b.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (nextBankAccount != null)
                    {
                        nextBankAccount.IsPrimary = true;

                        // Update user's recipient code
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            user.PaystackRecipientCode = nextBankAccount.PaystackRecipientCode;
                        }
                    }
                    else
                    {
                        // No more bank accounts, clear user's recipient code
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            user.PaystackRecipientCode = null;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bank account {bankAccountId} deleted for user {userId}");
                return (true, "Bank account deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting bank account {bankAccountId}");
                return (false, $"Error deleting bank account: {ex.Message}");
            }
        }

        private BankAccountResponseDto MapToDto(BankAccount bankAccount)
        {
            return new BankAccountResponseDto
            {
                Id = bankAccount.Id,
                UserId = bankAccount.UserId,
                AccountNumber = bankAccount.AccountNumber,
                BankName = bankAccount.BankName,
                BankCode = bankAccount.BankCode,
                AccountHolderName = bankAccount.AccountHolderName,
                AccountType = bankAccount.AccountType,
                IsVerified = bankAccount.IsVerified,
                IsPrimary = bankAccount.IsPrimary,
                PaystackRecipientCode = bankAccount.PaystackRecipientCode,
                CreatedAt = bankAccount.CreatedAt,
                UpdatedAt = bankAccount.UpdatedAt,
                VerificationFailureReason = bankAccount.VerificationFailureReason
            };
        }
    }
}
