namespace backend.Model
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }  // e.g. "Buyer", "Seller", "Admin"

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
