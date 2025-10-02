using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Model
{
    public class CartItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? TotalPrice { get; private set; }

        // FK for Product
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // FK for Cart
        public int CartId { get; set; }
        public Cart? Cart { get; set; }

        public void UpdateTotalPrice()
        {
            TotalPrice = UnitPrice * Quantity;
        }
        public void SetTotalPrice() => UpdateTotalPrice();
    }
}
