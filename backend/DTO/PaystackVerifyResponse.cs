namespace backend.DTO
{
    public class PaystackVerifyResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = null!;
        public PaystackVerifyData? Data { get; set; }
    }

    public class PaystackVerifyData
    {
        public string Status { get; set; } = null!;
        public string Reference { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Gateway_response { get; set; } = null!;
        public DateTime Paid_at { get; set; }
        public DateTime Created_at { get; set; }
        public string Channel { get; set; } = null!;
        public string Currency { get; set; } = null!;
        public string Ip_address { get; set; } = null!;
        public PaystackCustomer? Customer { get; set; }
    }

    public class PaystackCustomer
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string? Customer_code { get; set; }
    }
}
