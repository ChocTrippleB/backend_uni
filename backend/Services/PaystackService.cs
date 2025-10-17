using backend.DTO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace backend.Services
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private const string BaseUrl = "https://api.paystack.co";

        public PaystackService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _secretKey = configuration["Paystack:SecretKey"]
                ?? throw new InvalidOperationException("Paystack:SecretKey not configured");

            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _secretKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<PaystackInitializeResponse?> InitializePaymentAsync(
            string email,
            decimal amount,
            string reference)
        {
            try
            {
                // Paystack accepts amount in kobo (pesewa/cents), so multiply by 100
                var requestBody = new
                {
                    email,
                    amount = (int)(amount * 100), // Convert to kobo
                    reference,
                    currency = "ZAR", // South African Rand
                    callback_url = "https://yourdomain.com/payment/callback" // TODO: Update with your domain
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transaction/initialize", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PaystackInitializeResponse>(
                        responseString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Paystack Initialize Error: {ex.Message}");
                return null;
            }
        }

        public async Task<PaystackVerifyResponse?> VerifyPaymentAsync(string reference)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/transaction/verify/{reference}");
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PaystackVerifyResponse>(
                        responseString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Paystack Verify Error: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> CreateTransferRecipientAsync(
            string accountNumber,
            string bankCode,
            string accountName)
        {
            try
            {
                var requestBody = new
                {
                    type = "bank_account", // For South African bank accounts
                    name = accountName,
                    account_number = accountNumber,
                    bank_code = bankCode,
                    currency = "ZAR"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transferrecipient", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseString);
                    if (result.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("recipient_code", out var recipientCode))
                    {
                        return recipientCode.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Paystack Create Recipient Error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> InitiateTransferAsync(
            string recipientCode,
            decimal amount,
            string reference)
        {
            try
            {
                var requestBody = new
                {
                    source = "balance",
                    reason = "Payment for sold item",
                    amount = (int)(amount * 100), // Convert to kobo
                    recipient = recipientCode,
                    reference
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transfer", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Paystack Transfer Error: {ex.Message}");
                return false;
            }
        }
    }
}
