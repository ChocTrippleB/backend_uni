namespace backend.DTO
{
    public class CreateItemDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Condition { get; set; }
        public string Brand { get; set; }

        public int SellerId { get; set; }

        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}
