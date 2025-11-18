namespace backend.Model
{
    public class Cart
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; private set; }

        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public void AddItem(CartItem item)
        {
            var existing = Items.FirstOrDefault(i => i.ProductId == item.ProductId);

            if (existing != null)
            {
                existing.Quantity += item.Quantity;
                existing.UpdateTotalPrice();
            }
            else
            {
                item.UpdateTotalPrice();
                Items.Add(item);
                item.Cart = this;
            }

            UpdateTotalAmount();
        }

        public void RemoveItem(int productId)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                Items.Remove(item);
                UpdateTotalAmount();
            }
        }

        public void Clear()
        {
            Items.Clear();
            UpdateTotalAmount();
        }

        public void UpdateTotalAmount()
        {
            TotalAmount = Items.Sum(i => i.TotalPrice ?? 0m);
        }


    }
}
