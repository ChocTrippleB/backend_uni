namespace backend.DTO
{
    public class UpdateProductDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Condition { get; set; }
        public string? Brand { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }

        // we typically don’t allow changing SellerId on update 
        // but you can uncomment if needed
        // public int? SellerId { get; set; }

        // optional: allow updating images (replace or add)
        public List<IFormFile>? Images { get; set; }
    }

}
