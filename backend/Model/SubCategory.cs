namespace backend.Model
{
    public class SubCategory
    {
        public int Id { get; set; }
        public required string Name { get; set; }

        // Foreign key to Category
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
