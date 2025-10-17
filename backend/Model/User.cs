using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace backend.Model
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        //[MandelaOnly]
        public string Email { get; set; }

        [NotMapped]
        public string? Password { get; set; } // only used for registration/login, not saved

        [Required]
        public byte[] PasswordHash { get; set; }

        [Required]
        public byte[] PasswordSalt { get; set; }

        [Required]
        public string FullName { get; set; }  // ✅ NEW FIELD

        public bool EmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Paystack Integration Fields
        /// <summary>
        /// Paystack customer ID for this user
        /// </summary>
        public string? PaystackCustomerId { get; set; }

        /// <summary>
        /// Paystack recipient code for payouts (when user is a seller)
        /// </summary>
        public string? PaystackRecipientCode { get; set; }

        /// <summary>
        /// Phone number for payment notifications and order coordination
        /// </summary>
        public string? PhoneNumber { get; set; }

        // ✅ Listings posted by the user
        public List<Product> Items { get; set; }

        // ✅ Followers (users who follow this user)
        public List<UserFollower> Followers { get; set; }

        // ✅ Users this user is following
        public List<UserFollower> Followed { get; set; }

        // ✅ Orders where user is the buyer
        public List<Order> PurchaseOrders { get; set; }

        // ✅ Orders where user is the seller
        public List<Order> SaleOrders { get; set; }
    }
}
