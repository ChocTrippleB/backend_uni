using backend.Model;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace backend.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Follower> Followers { get; set; }
        public DbSet<UserFollower> UserFollowers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<PayoutQueue> PayoutQueue { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SellerRating> SellerRatings { get; set; }
        public DbSet<Address> Addresses { get; set; }

        // Wallet System
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<VoucherRedemption> VoucherRedemptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "User", Description = "Default role for all users", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = 2, Name = "Buyer", Description = "Legacy buyer role", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = 10, Name = "Admin", Description = "Full system access", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );


            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Books" },
                new Category { Id = 3, Name = "Clothing" },
                new Category { Id = 4, Name = "Furniture" },
                new Category { Id = 5, Name = "Appliances" },
                new Category { Id = 6, Name = "Fashion" },
                new Category { Id = 7, Name = "Accessories" },
                new Category { Id = 8, Name = "Audio" },
                new Category { Id = 9, Name = "Mobile" },
                new Category { Id = 10, Name = "Lifestyle" }
            );

            // Seed SubCategories
            modelBuilder.Entity<SubCategory>().HasData(
                // Electronics
                new SubCategory { Id = 1, Name = "Laptops", CategoryId = 1 },
                new SubCategory { Id = 2, Name = "Tablets", CategoryId = 1 },
                new SubCategory { Id = 3, Name = "Gaming", CategoryId = 1 },

                // Books
                new SubCategory { Id = 4, Name = "Textbooks", CategoryId = 2 },
                new SubCategory { Id = 5, Name = "Novels", CategoryId = 2 },

                // Clothing
                new SubCategory { Id = 6, Name = "Men's Clothes", CategoryId = 3 },
                new SubCategory { Id = 7, Name = "Women's Clothes", CategoryId = 3 },
                new SubCategory { Id = 8, Name = "Men's Shoes", CategoryId = 3 },
                new SubCategory { Id = 9, Name = "Women's Shoes", CategoryId = 3 },

                // Furniture
                new SubCategory { Id = 10, Name = "Desks", CategoryId = 4 },
                new SubCategory { Id = 11, Name = "Chairs", CategoryId = 4 },
                new SubCategory { Id = 12, Name = "Storage", CategoryId = 4 },

                // Appliances
                new SubCategory { Id = 13, Name = "Kitchen", CategoryId = 5 },
                new SubCategory { Id = 14, Name = "Cleaning", CategoryId = 5 },
                new SubCategory { Id = 15, Name = "Laundry", CategoryId = 5 },

                // Fashion
                new SubCategory { Id = 16, Name = "Watches", CategoryId = 6 },
                new SubCategory { Id = 17, Name = "Bags", CategoryId = 6 },
                new SubCategory { Id = 18, Name = "Sunglasses", CategoryId = 6 },

                // Accessories
                new SubCategory { Id = 19, Name = "Phone Accessories", CategoryId = 7 },
                new SubCategory { Id = 20, Name = "Laptop Accessories", CategoryId = 7 },
                new SubCategory { Id = 21, Name = "General Accessories", CategoryId = 7 },

                // Audio
                new SubCategory { Id = 22, Name = "Headphones", CategoryId = 8 },
                new SubCategory { Id = 23, Name = "Speakers", CategoryId = 8 },
                new SubCategory { Id = 24, Name = "Earbuds", CategoryId = 8 },

                // Mobile
                new SubCategory { Id = 25, Name = "Smartphones", CategoryId = 9 },
                new SubCategory { Id = 26, Name = "Feature Phones", CategoryId = 9 },
                new SubCategory { Id = 27, Name = "Smart Watches", CategoryId = 9 },

                // Lifestyle
                new SubCategory { Id = 28, Name = "Sports & Fitness", CategoryId = 10 },
                new SubCategory { Id = 29, Name = "Beauty & Health", CategoryId = 10 },
                new SubCategory { Id = 30, Name = "Home Decor", CategoryId = 10 }
            );

            modelBuilder.Entity<UserFollower>()
                .HasKey(uf => new { uf.FollowerId, uf.FollowedId });

            modelBuilder.Entity<UserFollower>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Followed)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserFollower>()
                .HasOne(uf => uf.Followed)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowedId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Order relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Buyer)
                .WithMany(u => u.PurchaseOrders)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Seller)
                .WithMany(u => u.SaleOrders)
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure PayoutQueue relationships
            modelBuilder.Entity<PayoutQueue>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PayoutQueue>()
                .HasOne(p => p.Seller)
                .WithMany()
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Add indexes for better query performance
            modelBuilder.Entity<PayoutQueue>()
                .HasIndex(p => new { p.Status, p.ScheduledPayoutDate });

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            // Configure BankAccount relationships
            modelBuilder.Entity<BankAccount>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for querying user's bank accounts
            modelBuilder.Entity<BankAccount>()
                .HasIndex(b => new { b.UserId, b.IsPrimary });

            // Configure Review relationships
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Buyer)
                .WithMany()
                .HasForeignKey(r => r.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Seller)
                .WithMany()
                .HasForeignKey(r => r.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // One review per order (unique constraint)
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.OrderId)
                .IsUnique();

            // Indexes for Review queries
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.SellerId);

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.BuyerId);

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.CreatedAt);

            // Configure SellerRating (SellerId is primary key)
            modelBuilder.Entity<SellerRating>()
                .HasOne(sr => sr.Seller)
                .WithMany()
                .HasForeignKey(sr => sr.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Address relationships
            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for querying user's addresses
            modelBuilder.Entity<Address>()
                .HasIndex(a => new { a.UserId, a.IsDefault });

            // Configure Wallet relationships (One-to-One: User-Wallet)
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: One wallet per user
            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId)
                .IsUnique();

            // Configure WalletTransaction relationships
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(wt => wt.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Order)
                .WithMany()
                .HasForeignKey(wt => wt.OrderId)
                .OnDelete(DeleteBehavior.SetNull); // Keep transaction history even if order is deleted

            // Indexes for WalletTransaction queries
            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(wt => wt.WalletId);

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(wt => wt.CreatedAt);

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(wt => wt.Reference)
                .IsUnique();

            // Configure VoucherRedemption relationships
            modelBuilder.Entity<VoucherRedemption>()
                .HasOne(vr => vr.User)
                .WithMany()
                .HasForeignKey(vr => vr.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VoucherRedemption>()
                .HasOne(vr => vr.Transaction)
                .WithMany()
                .HasForeignKey(vr => vr.TransactionId)
                .OnDelete(DeleteBehavior.NoAction);

            // Unique constraint: Prevent duplicate voucher redemptions
            modelBuilder.Entity<VoucherRedemption>()
                .HasIndex(vr => vr.VoucherHashedPin)
                .IsUnique();

            // Indexes for fraud detection and queries
            modelBuilder.Entity<VoucherRedemption>()
                .HasIndex(vr => vr.UserId);

            modelBuilder.Entity<VoucherRedemption>()
                .HasIndex(vr => vr.RedeemedAt);

            modelBuilder.Entity<VoucherRedemption>()
                .HasIndex(vr => vr.IsSuspicious);
        }
    }
}
