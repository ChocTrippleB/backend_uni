using backend.Model;

namespace backend.DTO
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }  // URL-friendly slug
        public string brand { get; set; }
        public decimal price { get; set; }
        public int inventory { get; set; }
        public string description { get; set; }
        public Category category { get; set; }
        public List<ImageDto> images { get; set; }
    }
}
