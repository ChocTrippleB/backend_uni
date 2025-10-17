namespace backend.DTO
{
    public class PaystackInitializeResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = null!;
        public PaystackData? Data { get; set; }
    }

    public class PaystackData
    {
        public string Authorization_url { get; set; } = null!;
        public string Access_code { get; set; } = null!;
        public string Reference { get; set; } = null!;
    }
}
