namespace backend.Model
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public string Condition { get; set; } = null!;
        public string Brand { get; set; } = null!;   // assuming brand
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public bool IsDeleted { get; set; } = false;  // soft delete
        public bool IsSold { get; set; } = false;  // ✅ Track if item is sold
        public DateTime? SoldAt { get; set; }  // ✅ When item was sold
        public int SellerId { get; set; }  // 🔥 add this
        public User Seller { get; set; } = null!; // optional nav property
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }



        public ICollection<Image> Images { get; set; } = new List<Image>();
        public SubCategory SubCategory { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }

}
