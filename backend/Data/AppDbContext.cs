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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Seller" },
                new Role { Id = 2, Name = "Buyer" },
                new Role { Id = 10, Name = "Admin" }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Books" },
                new Category { Id = 3, Name = "Clothing" },
                new Category { Id = 4, Name = "Furniture" },
                new Category { Id = 5, Name = "Appliances" }
            );

            // Seed SubCategories
            modelBuilder.Entity<SubCategory>().HasData(
                // Electronics
                new SubCategory { Id = 1, Name = "Laptops", CategoryId = 1 },
                new SubCategory { Id = 2, Name = "Phones", CategoryId = 1 },
                new SubCategory { Id = 3, Name = "Accessories", CategoryId = 1 },

                // Books
                new SubCategory { Id = 4, Name = "Textbooks", CategoryId = 2 },
                new SubCategory { Id = 5, Name = "Novels", CategoryId = 2 },

                // Clothing
                new SubCategory { Id = 6, Name = "Men's", CategoryId = 3 },
                new SubCategory { Id = 7, Name = "Women's", CategoryId = 3 },

                // Furniture
                new SubCategory { Id = 8, Name = "Desks", CategoryId = 4 },
                new SubCategory { Id = 9, Name = "Chairs", CategoryId = 4 },

                // Appliances
                new SubCategory { Id = 10, Name = "Kitchen", CategoryId = 5 },
                new SubCategory { Id = 11, Name = "Cleaning", CategoryId = 5 }
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
        }
    }
}
