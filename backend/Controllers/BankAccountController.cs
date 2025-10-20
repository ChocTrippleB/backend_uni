using backend.DTO;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class BankAccountController : ControllerBase
    {
        private readonly IBankAccountService _bankAccountService;
        private readonly ILogger<BankAccountController> _logger;

        public BankAccountController(
            IBankAccountService bankAccountService,
            ILogger<BankAccountController> logger)
        {
            _bankAccountService = bankAccountService;
            _logger = logger;
        }

        /// <summary>
        /// Add a new bank account for the current user
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddBankAccount([FromBody] AddBankAccountDto dto)
        {
            var userId = GetCurrentUserId();

            var (success, message, bankAccount) = await _bankAccountService.AddBankAccountAsync(userId, dto);

            if (!success)
                return BadRequest(new { message });

            return Ok(new
            {
                message,
                bankAccount
            });
        }

        /// <summary>
        /// Get all bank accounts for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyBankAccounts()
        {
            var userId = GetCurrentUserId();

            var bankAccounts = await _bankAccountService.GetUserBankAccountsAsync(userId);

            return Ok(bankAccounts);
        }

        /// <summary>
        /// Get the primary bank account for the current user
        /// </summary>
        [HttpGet("primary")]
        public async Task<IActionResult> GetPrimaryBankAccount()
        {
            var userId = GetCurrentUserId();

            var bankAccount = await _bankAccountService.GetPrimaryBankAccountAsync(userId);

            if (bankAccount == null)
                return NotFound(new { message = "No bank account found. Please add one." });

            return Ok(bankAccount);
        }

        /// <summary>
        /// Get a specific bank account by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBankAccountById(int id)
        {
            var userId = GetCurrentUserId();

            var bankAccount = await _bankAccountService.GetBankAccountByIdAsync(userId, id);

            if (bankAccount == null)
                return NotFound(new { message = "Bank account not found" });

            return Ok(bankAccount);
        }

        /// <summary>
        /// Update a bank account
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBankAccount(int id, [FromBody] AddBankAccountDto dto)
        {
            var userId = GetCurrentUserId();

            var (success, message) = await _bankAccountService.UpdateBankAccountAsync(userId, id, dto);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        /// <summary>
        /// Set a bank account as primary
        /// </summary>
        [HttpPatch("{id}/set-primary")]
        public async Task<IActionResult> SetPrimaryBankAccount(int id)
        {
            var userId = GetCurrentUserId();

            var (success, message) = await _bankAccountService.SetPrimaryBankAccountAsync(userId, id);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        /// <summary>
        /// Delete a bank account
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBankAccount(int id)
        {
            var userId = GetCurrentUserId();

            var (success, message) = await _bankAccountService.DeleteBankAccountAsync(userId, id);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userId;
        }
    }
}
